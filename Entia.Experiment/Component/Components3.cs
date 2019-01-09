using Entia.Core;
using Entia.Experiment.Modules;
using Entia.Experiment.Resolvables;
using Entia.Messages;
using Entia.Messages.Segment;
using Entia.Modules.Component;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    public sealed class Components3 : IModule
    {
        public sealed class Segment
        {
            public static readonly Segment Empty = new Segment(-1, new BitMask(), 0);

            public readonly int Index;
            public readonly BitMask Mask;
            public readonly (Metadata[] data, int minimum, int maximum) Components;
            public readonly (Metadata[] data, int minimum, int maximum) Tags;
            public (Entity[] items, int count) Entities;
            public Array[] Stores;

            public Segment(int index, BitMask mask, int capacity = 8)
            {
                Index = index;
                Mask = mask;

                var components = Mask
                    .Select(bit => ComponentUtility.TryGetMetadata(bit, out var data) ? data : default)
                    .Where(data => data.IsValid && !data.IsTag)
                    .ToArray();
                Components = (components, components.Select(data => data.Index).FirstOrDefault(), components.Select(data => data.Index + 1).LastOrDefault());
                var tags = Mask
                    .Select(bit => ComponentUtility.TryGetMetadata(bit, out var data) ? data : default)
                    .Where(data => data.IsValid && data.IsTag)
                    .ToArray();
                Tags = (tags, tags.Select(data => data.Index).FirstOrDefault(), tags.Select(data => data.Index + 1).LastOrDefault());

                Entities = (new Entity[capacity], 0);
                Stores = new Array[Components.maximum - Components.minimum];
                foreach (var datum in Components.data) Stores[StoreIndex(datum.Index)] = Array.CreateInstance(datum.Type, capacity);
            }

            public bool Has<T>() where T : struct, IComponent => Has(ComponentUtility.Cache<T>.Data.Index);
            public bool Has(int component) => Mask.Has(component);

            public bool TryStore<T>(out T[] store) where T : struct, IComponent
            {
                var index = StoreIndex<T>();
                if (index >= 0 && index < Stores.Length && Stores[index] is T[] casted)
                {
                    store = casted;
                    return true;
                }

                store = default;
                return false;
            }

            public ref Array Store(int component) => ref Stores[StoreIndex(component)];

            public bool TryStore(int component, out Array store)
            {
                var index = StoreIndex(component);
                if (index >= 0 && index < Stores.Length && Stores[index] is Array casted)
                {
                    store = casted;
                    return true;
                }

                store = default;
                return false;
            }

            int StoreIndex(int component) => component - Components.minimum;
            int StoreIndex<T>() where T : struct, IComponent => StoreIndex(ComponentUtility.Cache<T>.Data.Index);
        }

        readonly Messages _messages;
        readonly Resolvers _resolvers;

        ((Segment segment, int index)[] items, int count) _entityToSegment = (new (Segment, int)[64], 0);
        (Segment[] items, int count) _segments;
        readonly Segment _empty;
        readonly Dictionary<BitMask, Segment> _maskToSegment;
        readonly Dictionary<(Segment segment, int component), Segment> _transitions = new Dictionary<(Segment segment, int component), Segment>();

        public Components3(Messages messages, Resolvers resolvers)
        {
            _messages = messages;
            _resolvers = resolvers;

            _empty = new Segment(0, new BitMask());
            _segments = (new Segment[] { _empty }, 1);
            _maskToSegment = new Dictionary<BitMask, Segment> { { _empty.Mask, _empty } };
            // _messages.React((in OnCreate message) => Initialize(message.Entity));
            _messages.React((in OnPreDestroy message) => Dispose(message.Entity));
        }

        public ref T Write<T>(Entity entity) where T : struct, IComponent
        {
            if (TryGetSegment(entity, out var pair) && pair.segment.TryStore<T>(out var store)) return ref store[pair.index];
            if (_messages.Has<OnException>()) _messages.Emit(new OnException { Exception = ExceptionUtility.MissingComponent(entity, typeof(T)) });
            return ref Dummy<T>.Value;
        }

        public bool Has<T>(Entity entity) where T : struct, IComponent => Has(entity, ComponentUtility.Cache<T>.Data.Index);
        public bool Has(Entity entity, Type component) => ComponentUtility.TryGetMetadata(component, out var data) && Has(entity, data.Index);

        public bool Set<T>(Entity entity, in T component) where T : struct, IComponent
        {
            if (TryGetSegment(entity, out var pair) && pair.segment.TryStore<T>(out var store))
            {
                store[pair.index] = component;
                return false;
            }

            _resolvers.Defer(new Do<(Components3 @this, Entity entity, T component)>(
                (this, entity, component),
                state => state.@this.ResolveAdd<T>(state.entity, state.component)));
            return true;
        }

        public bool Remove<T>(Entity entity) where T : struct, IComponent
        {
            if (Has<T>(entity))
            {
                _resolvers.Defer(new Do<(Components3 @this, Entity entity)>(
                    (this, entity),
                    state => state.@this.ResolveRemove<T>(state.entity)));
                return true;
            }

            return false;
        }

        public bool Clear(Entity entity)
        {
            if (TryGetSegment(entity, out var pair) && pair.segment != _empty)
            {
                _resolvers.Defer(new Do<(Components3 @this, Entity entity)>(
                    (this, entity),
                    state => state.@this.ResolveClear(state.entity)));
                return true;
            }

            return false;
        }

        public bool TryGetSegment(Entity entity, out (Segment segment, int index) pair)
        {
            if (entity.Index < _entityToSegment.count)
            {
                pair = _entityToSegment.items[entity.Index];
                ref var entities = ref pair.segment.Entities;
                return pair.index < entities.count && entities.items[pair.index].Identifier == entity.Identifier;
            }

            pair = default;
            return false;
        }

        void ResolveAdd<T>(Entity entity, in T component) where T : struct, IComponent
        {
            if (TryGetSegment(entity, out var source))
            {
                // NOTE: check if a previous resolution already added the component
                if (source.segment.TryStore<T>(out var store)) store[source.index] = component;
                else
                {
                    var target = GetNextSegment<T>(source.segment, true);
                    var index = MoveTo(source, target);

                    target.TryStore<T>(out store);
                    store[index] = component;
                    MessageUtility.OnAddComponent<T>(_messages, entity);
                }
            }
        }

        void ResolveRemove<T>(Entity entity) where T : struct, IComponent
        {
            // NOTE: check if a previous resolution already removed the component
            if (TryGetSegment(entity, out var source) && source.segment.Has<T>())
            {
                var target = GetNextSegment<T>(source.segment, false);
                MoveTo(source, target);
                MessageUtility.OnRemoveComponent<T>(_messages, entity);
            }
        }

        void ResolveClear(Entity entity)
        {
            if (TryGetSegment(entity, out var source) && source.segment != _empty)
            {
                MoveTo(source, _empty);
                for (int i = 0; i < source.segment.Components.data.Length; i++)
                {
                    ref readonly var data = ref source.segment.Components.data[i];
                    MessageUtility.OnRemoveComponent(_messages, entity, data.Type, data.Index);
                }
            }
        }

        int MoveTo(in (Segment segment, int index) source, Segment target)
        {
            var index = target.Entities.count++;
            CopyTo(source, (target, index));
            // NOTE: copy the last entity to the moved entity's slot
            CopyTo((source.segment, --source.segment.Entities.count), source);
            return index;
        }

        void CopyTo(in (Segment segment, int index) source, in (Segment segment, int index) target)
        {
            if (source == target) return;

            var entity = source.segment.Entities.items[source.index];
            target.segment.Entities.Set(entity, target.index);
            _entityToSegment.items[entity.Index] = target;

            for (int i = 0; i < target.segment.Components.data.Length; i++)
            {
                ref readonly var data = ref target.segment.Components.data[i];
                ref var targetStore = ref target.segment.Store(data.Index);
                if (source.segment.TryStore(data.Index, out var sourceStore))
                {
                    ArrayUtility.Ensure(ref targetStore, data.Type, target.segment.Entities.items.Length);
                    Array.Copy(sourceStore, source.index, targetStore, target.index, 1);
                }
            }

            _messages.Emit(new OnMove { Source = source, Target = target });
        }

        bool Has(Entity entity, int component) => TryGetSegment(entity, out var pair) && pair.segment.Has(component);

        void Initialize(Entity entity)
        {
            var index = _empty.Entities.count;
            _empty.Entities.Set(entity, index);
            _entityToSegment.Set((_empty, index), entity.Index);
            // NOTE: no need to check the stores size since there are no stores in the empty segment
        }

        void Dispose(Entity entity)
        {
            ref var item = ref _entityToSegment.items[entity.Index];
            CopyTo((item.segment, --item.segment.Entities.count), item);
            item = default;
        }

        Segment GetNextSegment<T>(Segment segment, bool add) where T : struct, IComponent =>
            GetNextSegment(segment, ComponentUtility.Cache<T>.Data.Index, add);

        Segment GetNextSegment(Segment segment, int component, bool add)
        {
            var toKey = (segment, add ? component : ~component);
            if (_transitions.TryGetValue(toKey, out var next)) return next;

            var mask = new BitMask(segment.Mask);
            if (add) mask.Add(component);
            else mask.Remove(component);

            if (!_maskToSegment.TryGetValue(mask, out next))
                next = _maskToSegment[mask] = CreateSegment(mask);

            var fromKey = (next, ~component);
            _transitions[toKey] = next;
            _transitions[fromKey] = segment;
            return next;
        }

        Segment CreateSegment(BitMask mask)
        {
            var segment = new Segment(_segments.count, mask);
            _segments.Push(segment);
            _messages.Emit(new Entia.Messages.Segment.OnCreate { Segment = segment });
            return segment;
        }
    }
}