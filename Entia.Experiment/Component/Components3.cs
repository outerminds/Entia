using Entia.Core;
using Entia.Experiment.Modules;
using Entia.Experiment.Resolvables;
using Entia.Messages;
using Entia.Modules.Component;
using System;
using System.Collections.Generic;

namespace Entia.Modules
{
    public sealed class Components3 : IModule, IResolvable
    {
        struct Data
        {
            public Segment Segment;
            public int Index;
            public int? Transient;
        }

        readonly Transient _transient = new Transient();
        readonly Messages _messages;
        readonly Segment _empty;
        readonly Dictionary<BitMask, Segment> _maskToSegment;
        (Data[] items, int count) _data = (new Data[64], 0);
        (Segment[] items, int count) _segments;

        public Components3(Messages messages)
        {
            _messages = messages;
            _empty = new Segment(0, new BitMask());
            _segments = (new Segment[] { _empty }, 1);
            _maskToSegment = new Dictionary<BitMask, Segment> { { _empty.Mask, _empty } };
            _messages.React((in OnCreate message) => Initialize(message.Entity));
            _messages.React((in OnPreDestroy message) => Dispose(message.Entity));
        }

        public ref T Get<T>(Entity entity) where T : struct, IComponent
        {
            if (TryGetData(entity, out var data) && TryGetStore<T>(data, out var store, out var adjusted)) return ref store[adjusted];
            if (_messages.Has<OnException>()) _messages.Emit(new OnException { Exception = ExceptionUtility.MissingComponent(entity, typeof(T)) });
            return ref Dummy<T>.Value;
        }

        public ref T GetOrDummy<T>(Entity entity, out bool success) where T : struct, IComponent
        {
            if (TryGetData(entity, out var data) && TryGetStore<T>(data, out var store, out var adjusted))
            {
                success = true;
                return ref store[adjusted];
            }

            success = false;
            return ref Dummy<T>.Value;
        }

        public bool Has<T>(Entity entity) where T : struct, IComponent => Has(entity, ComponentUtility.Cache<T>.Data.Index);
        public bool Has(Entity entity, Type component) => ComponentUtility.TryGetMetadata(component, out var data) && Has(entity, data.Index);

        public bool Set<T>(Entity entity, in T component) where T : struct, IComponent
        {
            ref var data = ref GetData(entity, out var success);
            if (success)
            {
                var metadata = ComponentUtility.Cache<T>.Data;
                if (data.Segment.TryStore<T>(out var store))
                {
                    store[data.Index] = component;
                    return data.Transient is int transient && _transient.Entities.items[transient].mask.Add(metadata.Index);
                }

                var mask = GetTransientMask(entity, ref data, out var index);
                store = _transient.Store<T>(index, out var adjusted);
                store[adjusted] = component;
                return mask.Add(metadata.Index);
            }

            return false;
        }

        public bool Remove<T>(Entity entity) where T : struct, IComponent
        {
            ref var data = ref GetData(entity, out var success);
            var metadata = ComponentUtility.Cache<T>.Data;
            if (success && Has(data, metadata.Index))
            {
                var mask = GetTransientMask(entity, ref data, out _);
                return mask.Remove(metadata.Index);
            }

            return false;
        }

        public bool Clear(Entity entity)
        {
            ref var data = ref GetData(entity, out var success);
            if (success && data.Segment != _empty)
            {
                var mask = GetTransientMask(entity, ref data, out _);
                return mask.Clear();
            }

            return false;
        }

        public void Resolve()
        {
            for (int i = 0; i < _transient.Entities.count; i++)
            {
                ref var pair = ref _transient.Entities.items[i];
                ref var data = ref GetData(pair.entity, out var success);

                if (success)
                {
                    var segment = GetSegment(pair.mask);
                    MoveTo((data.Segment, data.Index), segment);
                }
            }

            _transient.Entities.count = 0;
        }

        ref Data GetData(Entity entity, out bool success)
        {
            if (entity.Index < _data.count)
            {
                ref var data = ref _data.items[entity.Index];
                if (data.Segment is Segment segment)
                {
                    ref var entities = ref data.Segment.Entities;
                    success = data.Index < entities.count && entities.items[data.Index].Identifier == entity.Identifier;
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
            data.Segment.TryStore(metadata, out store);

            if (data.Transient is int transient)
            {
                // NOTE: prioritize the segment store
                store = store ?? _transient.Store(transient, metadata, out adjusted);
                var mask = _transient.Entities.items[transient].mask;
                return mask.Has(metadata.Index);
            }

            return store != null;
        }

        BitMask GetTransientMask(Entity entity, ref Data data, out int index)
        {
            data.Transient = index = data.Transient ?? _transient.Reserve(entity, data.Segment.Mask);
            return _transient.Entities.items[index].mask;
        }

        int MoveTo(in (Segment segment, int index) source, Segment target)
        {
            var index = target.Entities.count++;
            CopyTo(source, (target, index));
            // NOTE: copy the last entity to the moved entity's slot
            CopyTo((source.segment, --source.segment.Entities.count), source);
            return index;
        }

        bool CopyTo((Segment segment, int index) source, in (Segment segment, int index) target)
        {
            if (source == target) return false;

            var entity = source.segment.Entities.items[source.index];
            target.segment.Entities.Set(target.index, entity);
            ref var data = ref _data.items[entity.Index];

            for (int i = 0; i < target.segment.Types.data.Length; i++)
            {
                ref readonly var metadata = ref target.segment.Types.data[i];
                ref var targetStore = ref target.segment.Store(metadata.Index);

                if (TryGetStore(data, metadata, out var sourceStore, out var sourceIndex))
                {
                    ArrayUtility.Ensure(ref targetStore, metadata.Type, target.segment.Entities.items.Length);
                    Array.Copy(sourceStore, sourceIndex, targetStore, target.index, 1);
                }
            }

            data.Segment = target.segment;
            data.Index = target.index;
            return true;
        }

        bool Has(Entity entity, int component)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Has(data, component);
        }

        bool Has(in Data data, int component)
        {
            if (data.Transient is int transient) return _transient.Entities.items[transient].mask.Has(component);
            return data.Segment.Has(component);
        }

        void Initialize(Entity entity)
        {
            var index = _empty.Entities.count;
            _empty.Entities.Set(index, entity);
            _data.Set(entity.Index, new Data { Segment = _empty, Index = index });
            // NOTE: no need to check the stores size since there are no stores in the empty segment
        }

        void Dispose(Entity entity)
        {
            ref var data = ref _data.items[entity.Index];
            CopyTo((data.Segment, --data.Segment.Entities.count), (data.Segment, data.Index));
            data = default;
        }

        Segment GetSegment(BitMask mask)
        {
            if (_maskToSegment.TryGetValue(mask, out var segment)) return segment;
            var clone = new BitMask { mask };
            segment = _maskToSegment[clone] = _segments.Push(new Segment(_segments.count, mask));
            _messages.Emit(new Entia.Messages.Segment.OnCreate { Segment = segment });
            return segment;
        }
    }
}