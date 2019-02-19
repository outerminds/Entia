using Entia.Core;
using Entia.Core.Documentation;
using Entia.Messages;
using Entia.Modules.Component;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Modules
{
    /// <summary>
    /// Module that stores and manages components.
    /// </summary>
    public sealed class Components : IModule, IResolvable, IEnumerable<IComponent>
    {
        struct Data
        {
            public bool IsValid => Segment != null;

            public Segment Segment;
            public int Index;
            public int? Transient;
        }

        /// <summary>
        /// Gets all the component segments.
        /// </summary>
        /// <value>
        /// The segments.
        /// </value>
        [ThreadSafe]
        public Slice<Segment>.Read Segments => _segments.Slice();

        readonly Entities _entities;
        readonly Messages _messages;
        readonly Transient _transient = new Transient();
        readonly Segment _created = new Segment(int.MaxValue, new BitMask());
        readonly Segment _destroyed = new Segment(int.MaxValue, new BitMask(), 1);
        readonly Segment _empty = new Segment(0, new BitMask());
        readonly Dictionary<BitMask, Segment> _maskToSegment;
        (Data[] items, int count) _data = (new Data[64], 0);
        (Segment[] items, int count) _segments;

        /// <summary>
        /// Initializes a new instance of the <see cref="Components"/> class.
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="messages"></param>
        public Components(Entities entities, Messages messages)
        {
            _entities = entities;
            _messages = messages;
            // NOTE: do not include '_created' and '_destroyed' here
            _segments = (new Segment[] { _empty }, 1);
            _maskToSegment = new Dictionary<BitMask, Segment> { { _empty.Mask, _empty } };
            _messages.React((in OnCreate message) => Initialize(message.Entity));
            _messages.React((in OnPostDestroy message) => Dispose(message.Entity));
            foreach (var entity in entities) Initialize(entity);
        }

        /// <summary>
        /// Gets a default component of type <typeref name="T"/>.
        /// If the component type has a <c>static</c> field, property or method tagged with the <see cref="Entia.DefaultAttribute"/> attribute, this member will be used to instantiate the component.
        /// </summary>
        /// <returns>The default component.</returns>
        [ThreadSafe]
        public T Default<T>() where T : struct, IComponent => DefaultUtility.Cache<T>.Provide();

        /// <summary>
        /// Gets a default component of provided <paramref name="type"/>.
        /// If the component type has a <c>static</c> field, property or method tagged with the <see cref="Entia.DefaultAttribute"/> attribute, this member will be used to instantiate the component.
        /// </summary>
        /// <param name="type">The concrete component type.</param>
        /// <param name="component">The default component.</param>
        /// <returns>Returns <c>true</c> if a valid component was created; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool TryDefault(Type type, out IComponent component)
        {
            if (ComponentUtility.TryGetMetadata(type, out _) && DefaultUtility.Default(type) is IComponent casted)
            {
                component = casted;
                return true;
            }

            component = default;
            return false;
        }

        /// <summary>
        /// Gets a component of type <typeref name="T"/> associated with the entity <paramref name="entity"/>.
        /// If the component is missing, a <see cref="OnException"/> message will be emitted.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <param name="entity">The entity associated with the component.</param>
        /// <returns>The component reference or <see cref="Dummy{T}.Value"/> if the component is missing.</returns>
        [ThreadSafe]
        public ref T Get<T>(Entity entity) where T : struct, IComponent
        {
            if (TryStore<T>(entity, out var store, out var adjusted)) return ref store[adjusted];
            if (_messages.Has<OnException>()) _messages.Emit(new OnException { Exception = ExceptionUtility.MissingComponent(entity, typeof(T)) });
            return ref Dummy<T>.Value;
        }

        /// <summary>
        /// Gets a component of type <typeref name="T"/> associated with the <paramref name="entity"/>.
        /// If the component is missing, a dummy reference will be returned.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <param name="entity">The entity associated with the component.</param>
        /// <param name="success">Is <c>true</c> if the component was found; otherwise, <c>false</c>.</param>
        /// <returns>The component reference or <see cref="Dummy{T}.Value"/> if the component is missing.</returns>
        [ThreadSafe]
        public ref T GetOrDummy<T>(Entity entity, out bool success) where T : struct, IComponent
        {
            if (TryStore<T>(entity, out var store, out var adjusted))
            {
                success = true;
                return ref store[adjusted];
            }

            success = false;
            return ref Dummy<T>.Value;
        }

        /// <summary>
        /// Gets a component of type <typeref name="T"/> associated with the <paramref name="entity"/>.
        /// If the component is missing, a new instance will be created using the <paramref name="create"/> function.
        /// If the <paramref name="create"> function is omitted, the default provider will be used.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <param name="entity">The entity associated with the component.</param>
        /// <param name="create">A function that creates a component of type <typeparamref name="T"/>.</param>
        /// <returns>The existing or added component reference.</returns>
        public ref T GetOrAdd<T>(Entity entity, Func<T> create = null) where T : struct, IComponent
        {
            if (TryStore<T>(entity, out var store, out var adjusted)) return ref store[adjusted];
            if (create == null) Set<T>(entity);
            else Set(entity, create());
            return ref Get<T>(entity);
        }

        /// <summary>
        /// Tries to get a component of type <typeparamref name="T"/> associated with the <paramref name="entity"/>.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="component">The component.</param>
        /// <returns>Returns <c>true</c> if the component was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool TryGet<T>(Entity entity, out T component) where T : struct, IComponent
        {
            component = GetOrDummy<T>(entity, out var success);
            return success;
        }

        /// <summary>
        /// Tries to get a component of provided <paramref name="type"/> associated with the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="type">The concrete component type.</param>
        /// <param name="component">The component.</param>
        /// <returns>Returns <c>true</c> if the component was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool TryGet(Entity entity, Type type, out IComponent component)
        {
            if (TryStore(entity, type, out var store, out var index))
            {
                component = (IComponent)store.GetValue(index);
                return true;
            }

            component = default;
            return false;
        }

        /// <summary>
        /// Gets a component of provided <paramref name="type"/> associated with the entity <paramref name="entity"/>.
        /// If the component is missing, a <see cref="OnException"/> message will be emitted.
        /// </summary>
        /// <param name="entity">The entity associated with the component.</param>
        /// <param name="type">The concrete component type.</param>
        /// <returns>The component or null if the component is missing.</returns>
        [ThreadSafe]
        public IComponent Get(Entity entity, Type type)
        {
            if (TryGet(entity, type, out var component)) return component;
            if (_messages.Has<OnException>()) _messages.Emit(new OnException { Exception = ExceptionUtility.MissingComponent(entity, type) });
            return null;
        }

        /// <summary>
        /// Gets all the components associated with the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity associated with the components.</param>
        /// <returns>The components.</returns>
        public IEnumerable<IComponent> Get(Entity entity)
        {
            ref var data = ref GetData(entity, out var success);
            return success ? Get(data) : Array.Empty<IComponent>();
        }

        /// <summary>
        /// Gets all entity-component pairs that have a component of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <returns>The entity-component pairs.</returns>
        [ThreadSafe]
        public IEnumerable<(Entity entity, T component)> Get<T>() where T : struct, IComponent
        {
            for (var i = 0; i < _data.count; i++)
            {
                var data = _data.items[i];
                if (data.IsValid && TryGetStore<T>(data, out var store, out var index))
                    yield return (data.Segment.Entities.items[data.Index], store[index]);
            }
        }

        /// <summary>
        /// Gets all entity-component pairs that have a component of provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The concrete component type.</param>
        /// <returns>The entity-component pairs.</returns>
        [ThreadSafe]
        public IEnumerable<(Entity entity, IComponent component)> Get(Type type)
        {
            if (ComponentUtility.TryGetMetadata(type, out var metadata))
            {
                for (var i = 0; i < _data.count; i++)
                {
                    var data = _data.items[i];
                    if (data.IsValid && TryGetStore(data, metadata, out var store, out var index))
                        yield return (data.Segment.Entities.items[data.Index], (IComponent)store.GetValue(index));
                }
            }
        }

        /// <summary>
        /// Determines whether the <paramref name="entity"/> has a component of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>Returns <c>true</c> if the component was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool Has<T>(Entity entity) where T : IComponent => ComponentUtility.Abstract<T>.IsConcrete ?
            Has(entity, ComponentUtility.Abstract<T>.Data.Index) :
            Has(entity, ComponentUtility.Abstract<T>.Mask);

        /// <summary>
        /// Determines whether the <paramref name="entity"/> has a component of provided <paramref name="type"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="type">The component type.</param>
        /// <returns>Returns <c>true</c> if the component was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool Has(Entity entity, Type type) =>
            ComponentUtility.TryGetMetadata(type, out var metadata) ? Has(entity, metadata.Index) :
            ComponentUtility.TryGetConcrete(type, out var mask) && Has(entity, mask);

        /// <summary>
        /// Counts the components associated with the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The number of components.</returns>
        public int Count(Entity entity)
        {
            ref var data = ref GetData(entity, out var success);
            return success ? GetTargetSegment(data).Types.data.Length : 0;
        }

        /// <summary>
        /// Counts all the components of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>The number of components.</returns>
        [ThreadSafe]
        public int Count<T>() where T : IComponent => ComponentUtility.Abstract<T>.IsConcrete ?
            Count(ComponentUtility.Abstract<T>.Data.Index) :
            Count(ComponentUtility.Abstract<T>.Mask);

        /// <summary>
        /// Counts all the components of provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The component type.</param>
        /// <returns>The number of components.</returns>
        [ThreadSafe]
        public int Count(Type type) =>
            ComponentUtility.TryGetMetadata(type, out var metadata) ? Count(metadata.Index) :
            ComponentUtility.TryGetConcrete(type, out var mask) ? Count(mask) :
            0;

        /// <summary>
        /// Sets a default component of type <typeparamref name="T"/> associated with the <paramref name="entity"/>.
        /// If the component is missing, it is added.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>Returns <c>true</c> if the component was added; otherwise, <c>false</c>.</returns>
        public bool Set<T>(Entity entity) where T : struct, IComponent => Set(entity, Default<T>());

        /// <summary>
        /// Sets the <paramref name="component"/> of type <typeparamref name="T"/> associated with the <paramref name="entity"/>.
        /// If the component is missing, it is added.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="component">The component.</param>
        /// <returns>Returns <c>true</c> if the component was added; otherwise, <c>false</c>.</returns>
        public bool Set<T>(Entity entity, in T component) where T : struct, IComponent
        {
            ref var data = ref GetData(entity, out var success);
            if (success)
            {
                ref readonly var metadata = ref ComponentUtility.Concrete<T>.Data;
                if (data.Segment.TryStore<T>(out var store))
                {
                    store[data.Index] = component;
                    if (data.Transient is int transient && _transient.Slots.items[transient].Mask.Add(metadata.Index))
                    {
                        ComponentUtility.Concrete<T>.OnAdd(_messages, entity);
                        return true;
                    }

                    return false;
                }

                ref var slot = ref GetTransientSlot(entity, ref data);
                if (slot.Resolution == Transient.Resolutions.Remove) return false;

                var index = data.Transient.Value;
                store = _transient.Store<T>(index, out var adjusted);
                store[adjusted] = component;

                if (slot.Mask.Add(metadata.Index))
                {
                    ComponentUtility.Concrete<T>.OnAdd(_messages, entity);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets a default component of provided type <paramref name="type"/> associated with the <paramref name="entity"/>.
        /// If the component is missing, it is added.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="type">The concrete component type.</typeparam>
        /// <returns>Returns <c>true</c> if the component was added; otherwise, <c>false</c>.</returns>
        public bool Set(Entity entity, Type type) => TryDefault(type, out var component) && Set(entity, component);

        /// <summary>
        /// Sets the <paramref name="component"/> associated with the <paramref name="entity"/>.
        /// If the component is missing, it is added.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="component">The component.</param>
        /// <returns>Returns <c>true</c> if the component was added; otherwise, <c>false</c>.</returns>
        public bool Set(Entity entity, IComponent component)
        {
            if (component == null) return false;

            ref var data = ref GetData(entity, out var success);
            if (success && ComponentUtility.TryGetMetadata(component.GetType(), out var metadata))
            {
                if (data.Segment.TryStore(metadata.Index, out var store))
                {
                    store.SetValue(component, data.Index);
                    if (data.Transient is int transient && _transient.Slots.items[transient].Mask.Add(metadata.Index))
                    {
                        MessageUtility.OnAdd(_messages, entity, metadata);
                        return true;
                    }

                    return false;
                }

                ref var slot = ref GetTransientSlot(entity, ref data);
                if (slot.Resolution == Transient.Resolutions.Remove) return false;

                var index = data.Transient.Value;
                store = _transient.Store(index, metadata, out var adjusted);
                store.SetValue(component, adjusted);

                if (slot.Mask.Add(metadata.Index))
                {
                    MessageUtility.OnAdd(_messages, entity, metadata);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes components of type <typeparamref name="T"/> associated with the <paramref name="entity"/>.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>Returns <c>true</c> if the component was removed; otherwise, <c>false</c>.</returns>
        public bool Remove<T>(Entity entity) where T : IComponent
        {
            ref var data = ref GetData(entity, out var success);
            if (success) return ComponentUtility.Abstract<T>.IsConcrete ?
                Remove(entity, ref data, ComponentUtility.Abstract<T>.Data, ComponentUtility.Abstract<T>.OnRemove) :
                Remove(entity, ref data, ComponentUtility.Abstract<T>.Mask);
            return false;
        }

        /// <summary>
        /// Removes components of provided <paramref name="type"/> associated with the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="type">The component type.</param>
        /// <returns>Returns <c>true</c> if the component was removed; otherwise, <c>false</c>.</returns>
        public bool Remove(Entity entity, Type type)
        {
            ref var data = ref GetData(entity, out var success);
            if (success) return
                ComponentUtility.TryGetMetadata(type, out var metadata) ? Remove(entity, ref data, metadata, MessageUtility.OnRemove(metadata)) :
                ComponentUtility.TryGetConcrete(type, out var mask) && Remove(entity, ref data, mask);
            return false;
        }

        /// <summary>
        /// Clears all components of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>Returns <c>true</c> if components were cleared; otherwise, <c>false</c>.</returns>
        public bool Clear<T>() where T : IComponent => ComponentUtility.Abstract<T>.IsConcrete ?
            Clear(ComponentUtility.Abstract<T>.Data, ComponentUtility.Abstract<T>.OnRemove) :
            Clear(ComponentUtility.Abstract<T>.Mask);

        /// <summary>
        /// Clears all components of provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The component type.</param>
        /// <returns>Returns <c>true</c> if components were cleared; otherwise, <c>false</c>.</returns>
        public bool Clear(Type type) =>
            ComponentUtility.TryGetMetadata(type, out var metadata) ? Clear(metadata, MessageUtility.OnRemove(metadata)) :
            ComponentUtility.TryGetConcrete(type, out var mask) && Clear(mask);

        /// <summary>
        /// Clears all components associated with the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Returns <c>true</c> if components were cleared; otherwise, <c>false</c>.</returns>
        public bool Clear(Entity entity)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Clear(entity, ref data);
        }

        /// <summary>
        /// Clears all components.
        /// </summary>
        /// <returns>Returns <c>true</c> if components were cleared; otherwise, <c>false</c>.</returns>
        public bool Clear()
        {
            var cleared = false;
            foreach (ref var data in _data.Slice()) cleared |= Clear(data.Segment.Entities.items[data.Index], ref data);
            return cleared;
        }

        /// <summary>
        /// Tries to get the component store of type <typeparamref name="T"/> associated with the <paramref name="entity"/>.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="store">The store.</param>
        /// <param name="index">The index in the store where the component is.</param>
        /// <returns>Returns <c>true</c> if the store was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool TryStore<T>(Entity entity, out T[] store, out int index) where T : struct, IComponent
        {
            ref readonly var data = ref GetData(entity, out var success);
            if (success && TryGetStore(data, out store, out index)) return true;
            store = default;
            index = default;
            return false;
        }

        /// <summary>
        /// Tries to get the component store of provided <paramref name="type"/> associated with the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="type">The concrete component type.</param>
        /// <param name="store">The store.</param>
        /// <param name="index">The index in the store where the component is.</param>
        /// <returns>Returns <c>true</c> if the store was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool TryStore(Entity entity, Type type, out Array store, out int index)
        {
            if (TryGetData(entity, out var data) &&
                ComponentUtility.TryGetMetadata(type, out var metadata) &&
                TryGetStore(data, metadata, out store, out index))
                return true;

            store = default;
            index = default;
            return false;
        }

        /// <summary>
        /// Tries the get segment associated with the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="pair">The segment and the index of the entity within it.</param>
        /// <returns>Returns <c>true</c> if the segment was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool TrySegment(Entity entity, out (Segment segment, int index) pair)
        {
            if (TryGetData(entity, out var data))
            {
                pair = (data.Segment, data.Index);
                return true;
            }

            pair = default;
            return false;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<IComponent> GetEnumerator()
        {
            foreach (var data in _data.Slice())
                if (data.IsValid) foreach (var component in Get(data)) yield return component;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerable<IComponent> Get(Data data)
        {
            var segment = GetTargetSegment(data);
            var types = segment.Types.data;
            for (var i = 0; i < types.Length; i++)
            {
                if (TryGetStore(data, types[i], out var store, out var index))
                    yield return (IComponent)store.GetValue(index);
            }
        }

        bool IResolvable.Resolve()
        {
            foreach (ref var slot in _transient.Slots.Slice())
            {
                ref var data = ref GetData(slot.Entity, out var success);
                if (success)
                {
                    switch (slot.Resolution)
                    {
                        case Transient.Resolutions.Add:
                            {
                                var segment = GetSegment(slot.Mask);
                                CopyTo((data.Segment, data.Index), (segment, segment.Entities.count++));
                                data.Transient = default;
                                break;
                            }
                        case Transient.Resolutions.Remove:
                            {
                                MoveTo((data.Segment, data.Index), _destroyed);
                                _destroyed.Entities.count = 0;
                                data = default;
                                break;
                            }
                        default:
                            {
                                var segment = GetSegment(slot.Mask);
                                MoveTo((data.Segment, data.Index), segment);
                                data.Transient = default;
                                break;
                            }
                    }
                }
            }

            return _created.Entities.count.Change(0) | _destroyed.Entities.count.Change(0) | _transient.Slots.count.Change(0);
        }

        [ThreadSafe]
        int Count(int index)
        {
            var count = 0;
            foreach (ref var data in _data.Slice()) if (data.IsValid && Has(data, index)) count++;
            return count;
        }

        [ThreadSafe]
        int Count(BitMask mask)
        {
            var count = 0;
            foreach (ref var data in _data.Slice()) if (data.IsValid && Has(data, mask)) count++;
            return count;
        }

        bool Clear(Entity entity, ref Data data)
        {
            if (data.Segment != _empty)
            {
                ref var slot = ref GetTransientSlot(entity, ref data);
                return slot.Mask.Clear();
            }

            return false;
        }

        bool Clear(in Metadata metadata, Action<Messages, Entity> onRemove)
        {
            var cleared = false;
            foreach (ref var data in _data.Slice())
                cleared |= data.IsValid && Remove(data.Segment.Entities.items[data.Index], ref data, metadata, onRemove);
            return cleared;
        }

        bool Clear(BitMask mask)
        {
            var cleared = false;
            foreach (ref var data in _data.Slice())
                cleared |= data.IsValid && Remove(data.Segment.Entities.items[data.Index], ref data, mask);
            return cleared;
        }

        bool Remove(Entity entity, ref Data data, in Metadata metadata, Action<Messages, Entity> onRemove)
        {
            if (Has(data, metadata.Index))
            {
                ref var slot = ref GetTransientSlot(entity, ref data);
                return Remove(entity, ref slot, metadata, onRemove);
            }

            return false;
        }

        bool Remove(Entity entity, ref Transient.Slot slot, in Metadata metadata, Action<Messages, Entity> onRemove)
        {
            if (slot.Mask.Remove(metadata.Index))
            {
                onRemove(_messages, entity);
                return true;
            }

            return false;
        }

        bool Remove(Entity entity, ref Data data, BitMask mask)
        {
            if (Has(data, mask))
            {
                ref var slot = ref GetTransientSlot(entity, ref data);
                return Remove(entity, ref slot, mask);
            }

            return false;
        }

        bool Remove(Entity entity, ref Transient.Slot slot, BitMask mask)
        {
            var removed = false;
            var segment = GetSegment(mask);
            var types = segment.Types.data;
            for (var i = 0; i < types.Length; i++)
            {
                ref readonly var metadata = ref types[i];
                removed |= Remove(entity, ref slot, metadata, MessageUtility.OnRemove(metadata));
            }

            return removed;
        }

        [ThreadSafe]
        ref Data GetData(Entity entity, out bool success)
        {
            if (entity.Index < _data.count)
            {
                ref var data = ref _data.items[entity.Index];
                if (data.Segment is Segment segment)
                {
                    ref var entities = ref segment.Entities;
                    success = data.Index < entities.count && entities.items[data.Index] == entity;
                    return ref data;
                }
            }

            success = false;
            return ref Dummy<Data>.Value;
        }

        [ThreadSafe]
        bool TryGetData(Entity entity, out Data data)
        {
            data = GetData(entity, out var success);
            return success;
        }

        [ThreadSafe]
        bool TryGetStore<T>(in Data data, out T[] store, out int adjusted) where T : struct, IComponent
        {
            if (TryGetStore(data, ComponentUtility.Concrete<T>.Data, out var array, out adjusted))
            {
                store = array as T[];
                return store != null;
            }

            store = default;
            return false;
        }

        [ThreadSafe]
        bool TryGetStore(in Data data, in Metadata metadata, out Array store, out int adjusted)
        {
            adjusted = data.Index;
            data.Segment.TryStore(metadata.Index, out store);

            if (data.Transient is int transient)
            {
                // NOTE: prioritize the segment store
                if (store == null) _transient.TryStore(transient, metadata, out store, out adjusted);
                ref readonly var slot = ref _transient.Slots.items[transient];
                // NOTE: if the slot has the component, then the store must not be null
                return Has(slot, metadata.Index);
            }

            return store != null;
        }

        int MoveTo(in (Segment segment, int index) source, Segment target)
        {
            if (source.segment == target) return source.index;

            var index = target.Entities.count++;
            CopyTo(source, (target, index));
            // NOTE: copy the last entity to the moved entity's slot
            CopyTo((source.segment, --source.segment.Entities.count), source);
            return index;
        }

        bool CopyTo(in (Segment segment, int index) source, in (Segment segment, int index) target)
        {
            if (source == target) return false;

            ref var entity = ref source.segment.Entities.items[source.index];
            if (entity == Entity.Zero) return false;

            ref var data = ref _data.items[entity.Index];
            if (target.segment.Entities.Set(target.index, entity)) target.segment.Ensure();

            var types = target.segment.Types.data;
            for (var i = 0; i < types.Length; i++)
            {
                ref readonly var metadata = ref types[i];
                var targetStore = target.segment.Store(metadata.Index);

                if (TryGetStore(data, metadata, out var sourceStore, out var sourceIndex))
                {
                    Array.Copy(sourceStore, sourceIndex, targetStore, target.index, 1);
                    // NOTE: clearing is not strictly needed, but is done when the component type contains managed references in order to allow
                    // them to be collected by the garbage collector
                    if (!metadata.IsPlain) Array.Clear(sourceStore, sourceIndex, 1);
                }
            }

            var message = new Entia.Messages.Segment.OnMove { Entity = entity, Source = source, Target = target };
            data.Segment = target.segment;
            data.Index = target.index;
            entity = default;
            _messages.Emit(message);
            return true;
        }

        [ThreadSafe]
        bool Has(Entity entity, BitMask mask)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Has(data, mask);
        }

        [ThreadSafe]
        bool Has(Entity entity, int index)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Has(data, index);
        }

        [ThreadSafe]
        bool Has(in Data data, BitMask mask)
        {
            if (data.Transient is int transient)
            {
                ref readonly var slot = ref _transient.Slots.items[transient];
                return Has(slot, mask);
            }
            return data.Segment.Mask.HasAny(mask);
        }

        [ThreadSafe]
        bool Has(in Data data, int index)
        {
            if (data.Transient is int transient)
            {
                ref readonly var slot = ref _transient.Slots.items[transient];
                return Has(slot, index);
            }
            return data.Segment.Mask.Has(index);
        }

        [ThreadSafe]
        bool Has(in Transient.Slot slot, BitMask mask) => slot.Resolution != Transient.Resolutions.Remove && slot.Mask.HasAny(mask);
        [ThreadSafe]
        bool Has(in Transient.Slot slot, int index) => slot.Resolution != Transient.Resolutions.Remove && slot.Mask.Has(index);

        void Initialize(Entity entity)
        {
            var transient = _transient.Reserve(entity, Transient.Resolutions.Add);
            var segment = _created;
            var index = segment.Entities.count++;
            segment.Entities.Ensure();
            segment.Entities.items[index] = entity;
            _data.Set(entity.Index, new Data { Segment = segment, Index = index, Transient = transient });
        }

        void Dispose(Entity entity)
        {
            ref var data = ref GetData(entity, out var success);
            if (success)
            {
                ref var slot = ref GetTransientSlot(entity, ref data);
                var segment = GetSegment(slot.Mask);
                var types = segment.Types.data;
                for (var i = 0; i < types.Length; i++)
                {
                    ref readonly var metadata = ref types[i];
                    Remove(entity, ref slot, metadata, MessageUtility.OnRemove(metadata));
                }

                slot.Resolution = Transient.Resolutions.Remove;
            }
        }

        Segment GetTargetSegment(in Data data) => data.Transient is int transient ? GetSegment(_transient.Slots.items[transient].Mask) : data.Segment;

        ref Transient.Slot GetTransientSlot(Entity entity, ref Data data)
        {
            if (data.Transient is int transient) return ref _transient.Slots.items[transient];
            data.Transient = transient = _transient.Reserve(entity, Transient.Resolutions.Move, data.Segment.Mask);
            return ref _transient.Slots.items[transient];
        }

        Segment GetSegment(BitMask mask)
        {
            if (_maskToSegment.TryGetValue(mask, out var segment)) return segment;
            var clone = new BitMask { mask };
            segment = _maskToSegment[clone] = _segments.Push(new Segment(_segments.count, clone));
            _messages.Emit(new Entia.Messages.Segment.OnCreate { Segment = segment });
            return segment;
        }
    }
}