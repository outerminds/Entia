using Entia.Core;
using Entia.Messages;
using Entia.Queryables;
using Entia.Segments;
using Entia.Stores;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    public sealed class Components : IModule, IResolvable, IEnumerable<IComponent>
    {
        sealed class Segment
        {
            public Type Type;
            public IStore[] Stores;
            public readonly BitMask Mask = new BitMask();

            public Segment(Type type, int capacity = 4)
            {
                Type = type;
                Stores = new IStore[capacity];
            }
        }

        readonly Entities _entities;
        readonly Stores _stores;
        readonly Messages _messages;
        (Segment[] items, int count) _segments = (new Segment[4], 0);

        public Components(Entities entities, Stores stores, Messages messages)
        {
            _entities = entities;
            _stores = stores;
            _messages = messages;
            _messages.React((in OnPreDestroy message) => Clear(message.Entity));
        }

        public ref T Write<T>(Entity entity) where T : struct, IComponent
        {
            if (TryGetBuffer<T>(entity, out var buffer, out var index)) return ref buffer[index];
            if (_messages.Has<OnException>()) _messages.Emit(new OnException { Exception = ExceptionUtility.MissingComponent(entity, typeof(T)) });
            return ref Dummy<T>.Value;
        }

        public bool TryWrite<T>(Entity entity, out Write<T> component) where T : struct, IComponent
        {
            if (TryGetBuffer<T>(entity, out var buffer, out var index))
            {
                component = new Write<T>(buffer, index);
                return true;
            }

            component = default;
            return false;
        }

        public ref T WriteOrAdd<T>(Entity entity, Func<T> create = null) where T : struct, IComponent
        {
            if (TryGetBuffer<T>(entity, out var buffer, out var index)) return ref buffer[index];
            Set(entity, create?.Invoke() ?? default);
            return ref Write<T>(entity);
        }

        public ref T WriteOrDummy<T>(Entity entity, out bool success) where T : struct, IComponent
        {
            if (TryGetBuffer<T>(entity, out var buffer, out var index))
            {
                success = true;
                return ref buffer[index];
            }

            success = false;
            return ref Dummy<T>.Value;
        }

        public ref readonly T Read<T>(Entity entity) where T : struct, IComponent => ref Write<T>(entity);

        public bool TryRead<T>(Entity entity, out Read<T> component) where T : struct, IComponent
        {
            if (TryGetBuffer<T>(entity, out var buffer, out var index))
            {
                component = new Read<T>(buffer, index);
                return true;
            }

            component = default;
            return false;
        }

        public ref readonly T ReadOrAdd<T>(Entity entity, Func<T> create = null) where T : struct, IComponent => ref WriteOrAdd(entity, create);

        public ref readonly T ReadOrDummy<T>(Entity entity, out bool success) where T : struct, IComponent => ref WriteOrDummy<T>(entity, out success);

        public bool TryGet<T>(Entity entity, out T component) where T : struct, IComponent
        {
            if (TryGetBuffer<T>(entity, out var buffer, out var index))
            {
                component = buffer[index];
                return true;
            }

            component = default;
            return false;
        }

        public bool TryGet(Entity entity, Type type, out IComponent component)
        {
            if (IndexUtility<IComponent>.TryGetIndex(type, out var index) && TryGet(entity, index.local, out component)) return true;
            component = default;
            return false;
        }

        public IEnumerable<IComponent> Get(Entity entity)
        {
            if (_entities.TryData(entity, out var data) && TryGetSegment(data.Segment.local, out var segment))
            {
                for (var i = 0; i < segment.Stores.Length; i++)
                {
                    if (segment.Stores[i] is IStore store && store.TryIndex(entity, out var adjusted) && store[adjusted] is IComponent component)
                        yield return component;
                }
            }
        }

        public IEnumerable<IComponent> Get(Type type)
        {
            if (IndexUtility<IComponent>.TryGetIndex(type, out var index))
                foreach (var entity in _entities) if (TryGet(entity, index.local, out var component)) yield return component;
        }

        public IEnumerable<T> Get<T>() where T : struct, IComponent
        {
            foreach (var entity in _entities) if (TryGet<T>(entity, out var component)) yield return component;
        }

        public bool Set<T>(Entity entity, in T component) where T : struct, IComponent
        {
            if (_entities.TryData(entity, out var data))
            {
                var (global, local) = IndexUtility<IComponent>.Cache<T>.Index;
                var segment = GetSegment(data.Segment.local);
                var store = GetStore<T>(segment);

                if (data.Mask.Add(global))
                {
                    store.Add(entity, component);
                    _messages.OnAddComponent<T>(entity);
                    return true;
                }

                return store.Set(entity, component);
            }

            return false;
        }

        public bool Set(Entity entity, IComponent component)
        {
            var type = component.GetType();
            if (_entities.TryData(entity, out var data) && IndexUtility<IComponent>.TryGetIndex(type, out var index))
            {
                var segment = GetSegment(data.Segment.local);
                var store = GetStore(segment, index, type);
                if (data.Mask.Add(index.global))
                {
                    store.Add(entity, component);
                    _messages.OnAddComponent(entity, type, index.local);
                    return true;
                }

                return store.Set(entity, component);
            }

            return false;
        }

        public int Count(Type type) => IndexUtility<IComponent>.TryGetIndex(type, out var index) ? Count(index.global) : 0;

        public int Count<T>() where T : struct, IComponent => Count(IndexUtility<IComponent>.Cache<T>.Index.global);

        public bool Has(Entity entity, Type type) => IndexUtility<IComponent>.TryGetIndex(type, out var index) && Has(entity, index.global);

        public bool Has<T>(Entity entity) where T : struct, IComponent => Has(entity, IndexUtility<IComponent>.Cache<T>.Index.global);

        public bool Remove(Entity entity, Type type)
        {
            if (IndexUtility<IComponent>.TryGetIndex(type, out var index) && Remove(entity, index))
            {
                _messages.OnRemoveComponent(entity, type, index.local);
                return true;
            }

            return false;
        }

        public bool Remove<T>(Entity entity) where T : struct, IComponent
        {
            var index = IndexUtility<IComponent>.Cache<T>.Index;
            if (Remove(entity, index))
            {
                _messages.OnRemoveComponent<T>(entity);
                return true;
            }

            return false;
        }

        public bool CopyTo(Entity source, Entity target, Components components)
        {
            if (_entities.TryData(source, out var sourceData) &&
                TryGetSegment(sourceData.Segment.local, out var sourceSegment) &&
                components._entities.TryData(target, out var targetData) &&
                components.TryGetSegment(targetData.Segment.local, out var targetSegment))
            {
                foreach (var (global, local, type) in IndexUtility<IComponent>.GetMaskTypes(sourceData.Mask))
                {
                    if (TryGetStore(sourceSegment, local, out var store))
                        store.CopyTo(source, target, components.GetStore(targetSegment, (global, local), type));
                }

                return true;
            }

            return false;
        }

        public bool Clear<T>() where T : struct, IComponent
        {
            var cleared = false;
            var (global, local) = IndexUtility<IComponent>.Cache<T>.Index;
            for (var i = 0; i < _segments.count; i++)
            {
                if (_segments.items[i] is Segment segment && TryGetStore<T>(segment, out var store))
                    cleared |= store.Clear();
            }

            if (cleared)
            {
                foreach (var (entity, data) in _entities.Pairs)
                    if (data.Mask.Remove(global)) _messages.OnRemoveComponent<T>(entity);
            }

            return cleared;
        }

        public bool Clear(Type type) => IndexUtility<IComponent>.TryGetIndex(type, out var index) && Clear(index, type);

        public bool Clear(Entity entity)
        {
            if (_entities.TryData(entity, out var data) && TryGetSegment(data.Segment.local, out var segment))
            {
                var cleared = false;
                foreach (var (global, local, type) in IndexUtility<IComponent>.GetMaskTypes(data.Mask))
                {
                    if (Remove(entity, segment, data.Mask, (global, local)))
                    {
                        cleared = true;
                        _messages.OnRemoveComponent(entity, type, local);
                    }
                }
                return cleared;
            }

            return false;
        }

        public bool Clear()
        {
            var cleared = _segments.count > 0;
            foreach (var (global, local, type) in IndexUtility<IComponent>.Types) cleared |= Clear((global, local), type);
            _segments.Clear();
            return cleared;
        }

        public IEnumerator<IComponent> GetEnumerator() => _segments.items
            .Take(_segments.count)
            .Some()
            .SelectMany(segment => segment.Stores)
            .Some()
            .SelectMany(store => store.Values)
            .OfType<IComponent>()
            .GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool TryGet(Entity entity, int local, out IComponent component)
        {
            if (_entities.TryData(entity, out var data) &&
                TryGetSegment(data.Segment.local, out var segment) &&
                TryGetStore(segment, local, out var store) &&
                store.TryIndex(entity, out var index))
            {
                component = store[index] as IComponent;
                return component != null;
            }

            component = default;
            return false;
        }

        bool Has(Entity entity, int global) => _entities.TryMask(entity, out var mask) && mask.Has(global);

        int Count(int global)
        {
            var count = 0;
            foreach (var entity in _entities) if (Has(entity, global)) count++;
            return count;
        }

        bool Remove(Entity entity, (int global, int local) index) =>
            _entities.TryData(entity, out var data) && TryGetSegment(data.Segment.local, out var segment) && Remove(entity, segment, data.Mask, index);

        bool Remove(Entity entity, Segment segment, BitMask mask, (int global, int local) index) =>
            mask.Remove(index.global) && TryGetStore(segment, index.local, out var store) && store.Remove(entity);

        bool Clear((int global, int local) index, Type type)
        {
            var cleared = false;
            for (var i = 0; i < _segments.count; i++)
            {
                if (_segments.items[i] is Segment segment && TryGetStore(segment, index.local, out var store))
                    cleared |= store.Clear();
            }

            if (cleared)
            {
                foreach (var (entity, data) in _entities.Pairs)
                    if (data.Mask.Remove(index.global)) _messages.OnRemoveComponent(entity, type, index.local);
            }

            return cleared;
        }

        bool TryGetSegment(int local, out Segment segment)
        {
            segment = local < _segments.count ? _segments.items[local] : default;
            return segment != null;
        }

        Segment GetSegment(int local)
        {
            if (TryGetSegment(local, out var segment)) return segment;
            IndexUtility<ISegment>.TryGetType(local, out var type);
            _segments.Set(segment = new Segment(type), local);
            return segment;
        }

        bool TryGetBuffer<T>(Entity entity, out T[] buffer, out int index) where T : struct, IComponent
        {
            if (_entities.TryData(entity, out var data) &&
                TryGetSegment(data.Segment.local, out var segment) &&
                TryGetStore<T>(segment, out var store) &&
                store.TryGet(entity, out buffer, out index))
                return true;

            buffer = default;
            index = default;
            return false;
        }

        bool TryGetStore(Segment segment, int local, out IStore store)
        {
            if (local < segment.Stores.Length)
            {
                store = segment.Stores[local];
                return store != null;
            }

            store = default;
            return false;
        }

        bool TryGetStore<T>(Segment segment, out Store<T> store) where T : struct, IComponent
        {
            var (_, local) = IndexUtility<IComponent>.Cache<T>.Index;
            if (local < segment.Stores.Length)
            {
                store = segment.Stores[local] as Store<T>;
                return store != null;
            }

            store = default;
            return false;
        }

        IStore GetStore(Segment segment, (int global, int local) index, Type type)
        {
            if (TryGetStore(segment, index.local, out var store)) return store;

            ArrayUtility.Ensure(ref segment.Stores, index.local + 1);
            segment.Stores[index.local] = store = _stores.Store(type, segment.Type);
            segment.Mask.Add(index.global);
            return store;
        }

        Store<T> GetStore<T>(Segment segment) where T : struct, IComponent
        {
            if (TryGetStore<T>(segment, out var store)) return store;

            var (global, local) = IndexUtility<IComponent>.Cache<T>.Index;
            ArrayUtility.Ensure(ref segment.Stores, local + 1);
            segment.Stores[local] = store = _stores.Store<T>(segment.Type);
            segment.Mask.Add(global);
            return store;
        }

        void IResolvable.Resolve()
        {
            for (var i = 0; i < _segments.count; i++)
            {
                if (_segments.items[i] is Segment segment)
                {
                    foreach (var store in segment.Stores)
                    {
                        if (store == null) continue;
                        if (store.Resolve()) _messages.Emit(new OnResolve { Store = store });
                    }
                }
            }
        }
    }
}