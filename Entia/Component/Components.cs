using Entia.Core;
using Entia.Messages;
using Entia.Modules.Component;
using Entia.Queryables;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Modules
{
    public sealed class Components : IModule, IResolvable, IEnumerable<IComponent>
    {
        struct Data
        {
            public bool IsValid => Segment != null;

            public Segment Segment;
            public int Index;
            public int? Transient;
        }

        public ArrayEnumerable<Segment> Segments => _segments.Enumerate();

        readonly Entities _entities;
        readonly Messages _messages;
        readonly Transient _transient = new Transient();
        readonly Segment _created = new Segment(int.MaxValue, new BitMask());
        readonly Segment _destroyed = new Segment(int.MaxValue, new BitMask(), 1);
        readonly Segment _empty = new Segment(0, new BitMask());
        readonly Dictionary<BitMask, Segment> _maskToSegment;
        (Data[] items, int count) _data = (new Data[64], 0);
        (Segment[] items, int count) _segments;

        public Components(Entities entities, Messages messages)
        {
            _entities = entities;
            _messages = messages;
            // NOTE: do not include '_pending' here
            _segments = (new Segment[] { _empty }, 1);
            _maskToSegment = new Dictionary<BitMask, Segment> { { _empty.Mask, _empty } };
            _messages.React((in OnCreate message) => Initialize(message.Entity));
            _messages.React((in OnPreDestroy message) => Dispose(message.Entity));
            foreach (var entity in entities) Initialize(entity);
        }

        public ref T Get<T>(Entity entity) where T : struct, IComponent
        {
            if (TryGetStore<T>(entity, out var store, out var adjusted)) return ref store[adjusted];
            if (_messages.Has<OnException>()) _messages.Emit(new OnException { Exception = ExceptionUtility.MissingComponent(entity, typeof(T)) });
            return ref Dummy<T>.Value;
        }

        public ref T GetOrDummy<T>(Entity entity, out bool success) where T : struct, IComponent
        {
            if (TryGetStore<T>(entity, out var store, out var adjusted))
            {
                success = true;
                return ref store[adjusted];
            }

            success = false;
            return ref Dummy<T>.Value;
        }

        public ref T GetOrAdd<T>(Entity entity, Func<T> create = null) where T : struct, IComponent
        {
            if (TryGetStore<T>(entity, out var store, out var adjusted)) return ref store[adjusted];
            Set(entity, create?.Invoke() ?? default);
            return ref Get<T>(entity);
        }

        public bool TryGet<T>(Entity entity, out T component) where T : struct, IComponent
        {
            component = GetOrDummy<T>(entity, out var success);
            return success;
        }

        public bool TryGet(Entity entity, Type type, out IComponent component)
        {
            if (TryGetStore(entity, type, out var store, out var index))
            {
                component = (IComponent)store.GetValue(index);
                return true;
            }

            component = default;
            return false;
        }

        public IComponent Get(Entity entity, Type type)
        {
            if (TryGet(entity, type, out var component)) return component;
            if (_messages.Has<OnException>()) _messages.Emit(new OnException { Exception = ExceptionUtility.MissingComponent(entity, type) });
            return null;
        }

        public IEnumerable<IComponent> Get(Entity entity)
        {
            ref var data = ref GetData(entity, out var success);
            if (success) return Get(data);
            return Array.Empty<IComponent>();
        }

        public IEnumerable<(Entity entity, T component)> Get<T>() where T : struct, IComponent
        {
            for (int i = 0; i < _data.count; i++)
            {
                var data = _data.items[i];
                if (data.IsValid && TryGetStore<T>(data, out var store, out var index))
                    yield return (data.Segment.Entities.items[data.Index], store[index]);
            }
        }

        public IEnumerable<(Entity entity, IComponent component)> Get(Type component)
        {
            if (ComponentUtility.TryGetMetadata(component, out var metadata))
            {
                for (int i = 0; i < _data.count; i++)
                {
                    var data = _data.items[i];
                    if (data.IsValid && TryGetStore(data, metadata, out var store, out var index))
                        yield return (data.Segment.Entities.items[data.Index], (IComponent)store.GetValue(index));
                }
            }
        }

        public bool Has<T>(Entity entity) where T : struct, IComponent => Has(entity, ComponentUtility.Cache<T>.Data.Index);

        public bool Has(Entity entity, Type component) => ComponentUtility.TryGetMetadata(component, out var data) && Has(entity, data.Index);

        public int Count(Entity entity)
        {
            ref var data = ref GetData(entity, out var success);
            if (success)
            {
                var segment = GetTargetSegment(data);
                return segment.Types.data.Length;
            }

            return 0;
        }

        public int Count<T>() where T : struct, IComponent => Count(ComponentUtility.Cache<T>.Data);

        public int Count(Type component) => ComponentUtility.TryGetMetadata(component, out var metadata) ? Count(metadata) : 0;

        public bool Set<T>(Entity entity, in T component) where T : struct, IComponent
        {
            ref var data = ref GetData(entity, out var success);
            if (success)
            {
                var metadata = ComponentUtility.Cache<T>.Data;
                if (data.Segment.TryGetStore<T>(out var store))
                {
                    store[data.Index] = component;
                    if (data.Transient is int transient && _transient.Slots.items[transient].Mask.Add(metadata.Index))
                    {
                        MessageUtility.OnAdd<T>(_messages, entity);
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
                    MessageUtility.OnAdd<T>(_messages, entity);
                    return true;
                }
            }

            return false;
        }

        public bool Set(Entity entity, IComponent component)
        {
            ref var data = ref GetData(entity, out var success);
            if (success && ComponentUtility.TryGetMetadata(component.GetType(), out var metadata))
            {
                if (data.Segment.TryGetStore(metadata, out var store))
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

        public bool Remove<T>(Entity entity) where T : struct, IComponent
        {
            ref var data = ref GetData(entity, out var success);
            return success && Remove(entity, ref data, ComponentUtility.Cache<T>.Data, MessageUtility.OnRemove<T>());
        }

        public bool Remove(Entity entity, Type component)
        {
            ref var data = ref GetData(entity, out var success);
            return success &&
                ComponentUtility.TryGetMetadata(component, out var metadata) &&
                Remove(entity, ref data, metadata, MessageUtility.OnRemove(metadata));
        }

        public bool Clear<T>() where T : struct, IComponent => Clear(ComponentUtility.Cache<T>.Data, MessageUtility.OnRemove<T>());

        public bool Clear(Type component) => ComponentUtility.TryGetMetadata(component, out var metadata) && Clear(metadata, MessageUtility.OnRemove(metadata));

        public bool Clear(Entity entity)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Clear(entity, ref data);
        }

        public bool Clear()
        {
            var cleared = false;
            foreach (ref var data in _data.Enumerate()) cleared |= Clear(data.Segment.Entities.items[data.Index], ref data);
            return cleared;
        }

        public void Resolve()
        {
            foreach (ref var slot in _transient.Slots.Enumerate())
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

            _created.Entities.count = 0;
            _destroyed.Entities.count = 0;
            _transient.Slots.count = 0;
        }

        public bool TryGetStore<T>(Entity entity, out T[] store, out int index) where T : struct, IComponent
        {
            if (TryGetData(entity, out var data) && TryGetStore<T>(data, out store, out index)) return true;
            store = default;
            index = default;
            return false;
        }

        public bool TryGetStore(Entity entity, Type type, out Array store, out int index)
        {
            if (TryGetData(entity, out var data) &&
                ComponentUtility.TryGetMetadata(type, out var metadata) &&
                TryGetStore(data, metadata, out store, out index))
                return true;

            store = default;
            index = default;
            return false;
        }

        public bool TryGetSegment(Entity entity, out (Segment segment, int index) pair)
        {
            if (TryGetData(entity, out var data))
            {
                pair = (data.Segment, data.Index);
                return true;
            }

            pair = default;
            return false;
        }

        public IEnumerator<IComponent> GetEnumerator()
        {
            foreach (var data in _data.Enumerate())
                if (data.IsValid) foreach (var component in Get(data)) yield return component;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerable<IComponent> Get(Data data)
        {
            var segment = GetTargetSegment(data);
            var types = segment.Types.data;
            for (int i = 0; i < types.Length; i++)
            {
                if (TryGetStore(data, types[i], out var store, out var index))
                    yield return (IComponent)store.GetValue(index);
            }
        }

        int Count(in Metadata metadata)
        {
            var count = 0;
            foreach (ref var data in _data.Enumerate()) if (data.IsValid && Has(data, metadata.Index)) count++;
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

        bool Clear(Metadata metadata, Action<Messages, Entity> onRemove)
        {
            var cleared = false;
            foreach (ref var data in _data.Enumerate())
                cleared |= data.IsValid && Remove(data.Segment.Entities.items[data.Index], ref data, metadata, onRemove);
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

        ref Data GetData(Entity entity, out bool success)
        {
            if (entity.Index < _data.count)
            {
                ref var data = ref _data.items[entity.Index];
                if (data.Segment is Segment segment)
                {
                    ref var entities = ref data.Segment.Entities;
                    success = data.Index < entities.count && entities.items[data.Index] == entity;
                    return ref data;
                }
            }

            success = false;
            return ref Dummy<Data>.Value;
        }

        bool TryGetData(Entity entity, out Data data)
        {
            data = GetData(entity, out var success);
            return success;
        }

        bool TryGetStore<T>(in Data data, out T[] store, out int adjusted) where T : struct, IComponent
        {
            if (TryGetStore(data, ComponentUtility.Cache<T>.Data, out var array, out adjusted))
            {
                store = array as T[];
                return store != null;
            }

            store = default;
            return false;
        }

        bool TryGetStore(in Data data, in Metadata metadata, out Array store, out int adjusted)
        {
            adjusted = data.Index;
            data.Segment.TryGetStore(metadata, out store);

            if (data.Transient is int transient)
            {
                // NOTE: prioritize the segment store
                store = store ?? _transient.Store(transient, metadata, out adjusted);
                ref readonly var slot = ref _transient.Slots.items[transient];
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
            target.segment.Entities.Set(target.index, entity);

            var types = target.segment.Types.data;
            for (int i = 0; i < types.Length; i++)
            {
                ref readonly var metadata = ref types[i];
                ref var targetStore = ref target.segment.GetStore(metadata.Index);
                ArrayUtility.Ensure(ref targetStore, metadata.Type, target.segment.Entities.items.Length);

                if (TryGetStore(data, metadata, out var sourceStore, out var sourceIndex))
                    Array.Copy(sourceStore, sourceIndex, targetStore, target.index, 1);
            }

            var message = new Entia.Messages.Segment.OnMove { Entity = entity, Source = source, Target = target };
            data.Segment = target.segment;
            data.Index = target.index;
            entity = default;
            _messages.Emit(message);
            return true;
        }

        bool Has(Entity entity, int component)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Has(data, component);
        }

        bool Has(in Data data, int component)
        {
            if (data.Transient is int transient)
            {
                ref readonly var slot = ref _transient.Slots.items[transient];
                return Has(slot, component);
            }
            return data.Segment.Has(component);
        }

        bool Has(in Transient.Slot slot, int component) => slot.Resolution != Transient.Resolutions.Remove && slot.Mask.Has(component);

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
                foreach (var metadata in segment.Types.data)
                    Remove(entity, ref slot, metadata, MessageUtility.OnRemove(metadata));
                slot.Resolution = Transient.Resolutions.Remove;
            }
        }

        Segment GetTargetSegment(in Data data) => data.Transient is int transient ? GetSegment(_transient.Slots.items[transient].Mask) : data.Segment;

        ref Transient.Slot GetTransientSlot(Entity entity, ref Data data)
        {
            var index = data.Transient ?? _transient.Reserve(entity, Transient.Resolutions.Move, data.Segment.Mask);
            data.Transient = index;
            return ref _transient.Slots.items[index];
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