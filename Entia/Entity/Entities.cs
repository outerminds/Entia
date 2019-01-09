using Entia.Core;
using Entia.Segments;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Modules
{
    public sealed class Entities : IModule, IResolvable, IEnumerable<Entity>
    {
        public struct Enumerator : IEnumerator<Entity>
        {
            public Entity Current => new Entity(_index, _entities._data.items[_index].Generation);
            object IEnumerator.Current => Current;

            Entities _entities;
            int _index;

            public Enumerator(Entities entities)
            {
                _entities = entities;
                _index = -1;
            }

            public bool MoveNext()
            {
                while (++_index < _entities._data.count)
                    if (_entities._data.items[_index].Allocated) return true;

                return false;
            }
            public void Reset() => _index = -1;
            public void Dispose() => _entities = null;
        }

        public struct SegmentEnumerator : IEnumerator<Entity>
        {
            public Entity Current => _segment.Entities.items[_index];
            object IEnumerator.Current => Current;

            SegmentData _segment;
            int _index;

            public SegmentEnumerator(SegmentData segment)
            {
                _segment = segment;
                _index = -1;
            }

            public bool MoveNext()
            {
                while (++_index < _segment.Entities.count)
                    if (_segment.Entities.items[_index] != Entity.Zero) return true;

                return false;
            }
            public void Reset() => _index = -1;
            public void Dispose() => _segment = default;
        }

        public readonly struct SegmentEnumerable : IEnumerable<Entity>
        {
            public static readonly SegmentEnumerable Empty = new SegmentEnumerable(SegmentData.Empty);

            readonly SegmentData _segment;

            public SegmentEnumerable(SegmentData segment) { _segment = segment; }

            public SegmentEnumerator GetEnumerator() => new SegmentEnumerator(_segment);
            IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct PairEnumerator : IEnumerator<(Entity entity, Data data)>
        {
            public (Entity entity, Data data) Current => (new Entity(_index, _entities._data.items[_index].Generation), _entities._data.items[_index]);
            object IEnumerator.Current => Current;

            Entities _entities;
            int _index;

            public PairEnumerator(Entities entities)
            {
                _entities = entities;
                _index = -1;
            }

            public bool MoveNext()
            {
                while (++_index < _entities._data.count)
                    if (_entities._data.items[_index].Allocated) return true;

                return false;
            }
            public void Reset() => _index = -1;
            public void Dispose() => _entities = null;
        }

        public readonly struct PairEnumerable : IEnumerable<(Entity entity, Data data)>
        {
            readonly Entities _entities;
            public PairEnumerable(Entities entities) { _entities = entities; }

            public PairEnumerator GetEnumerator() => new PairEnumerator(_entities);
            IEnumerator<(Entity entity, Data data)> IEnumerable<(Entity entity, Data data)>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public sealed class SegmentData : IEnumerable<Entity>
        {
            public static readonly SegmentData Empty = new SegmentData(0);

            public int Count => Entities.count - Free.count - Frozen.count;

            public (Entity[] items, int count) Entities;
            public (int[] items, int count) Free = (new int[8], 0);
            public (int[] items, int count) Frozen = (new int[8], 0);

            public SegmentData(int capacity = 8) { Entities = (new Entity[capacity], 0); }

            public SegmentEnumerator GetEnumerator() => new SegmentEnumerator(this);
            IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct Data
        {
            public (int global, int local) Segment;
            public int Index;
            public uint Generation;
            public bool Allocated;
            public BitMask Mask;
        }

        public int Capacity => _data.items.Length;
        public PairEnumerable Pairs => new PairEnumerable(this);

        readonly Messages _messages;
        (int[] items, int count) _free = (new int[8], 0);
        (int[] items, int count) _frozen = (new int[8], 0);
        (Data[] items, int count) _data = (new Data[64], 0);
        (SegmentData[] items, int count) _segments = (new SegmentData[4], 0);

        public Entities(Messages messages) { _messages = messages; }

        public Entity Create() => Create<Default>();

        public Entity Create<T>() where T : struct, ISegment
        {
            var index = IndexUtility<ISegment>.Cache<T>.Index;
            return Create(index, MessageUtility.OnCreateEntity<T>());
        }

        public Entity Create(Type segment) =>
            IndexUtility<ISegment>.TryGetIndex(segment, out var index) ? Create(index, MessageUtility.OnCreateEntity(segment, index.local)) : Create();

        public bool Destroy(Entity entity)
        {
            if (TryData(entity, out var data) && IndexUtility<ISegment>.TryGetType(data.Segment.local, out var segment))
            {
                Destroy(entity, data, MessageUtility.OnPreDestroyEntity(segment, data.Segment.local), MessageUtility.OnPostDestroyEntity(segment, data.Segment.local));
                return true;
            }

            return false;
        }

        public bool Destroy<T>(Entity entity) where T : struct, ISegment
        {
            var index = IndexUtility<ISegment>.Cache<T>.Index;
            if (TryData(entity, out var data) && data.Segment.global == index.global)
            {
                Destroy(entity, data, MessageUtility.OnPreDestroyEntity<T>(), MessageUtility.OnPostDestroyEntity<T>());
                return true;
            }

            return false;
        }

        public bool Destroy(Entity entity, Type segment)
        {
            if (TryData(entity, out var data) && IndexUtility<ISegment>.TryGetIndex(segment, out var index) && data.Segment.global == index.global)
            {
                Destroy(entity, data, MessageUtility.OnPreDestroyEntity(segment, index.local), MessageUtility.OnPostDestroyEntity(segment, index.local));
                return true;
            }

            return false;
        }

        public bool Has(Entity entity) => TryData(entity, out _);

        public bool Has<T>(Entity entity) where T : struct, ISegment =>
            TryData(entity, out var data) && data.Segment.global == IndexUtility<ISegment>.Cache<T>.Index.global;

        public bool Has(Entity entity, Type segment) =>
            TryData(entity, out var data) && IndexUtility<ISegment>.TryGetIndex(segment, out var index) && data.Segment.global == index.global;

        public int Count() => _data.count - _free.count - _frozen.count;

        public int Count<T>() where T : struct, ISegment => TryGetSegment<T>(out var segment) ? segment.Count : 0;

        public int Count(Type segment) => TryGetSegment(segment, out var data) ? data.Count : 0;

        public SegmentEnumerable Get<T>() where T : struct, ISegment => TryGetSegment<T>(out var segment) ? new SegmentEnumerable(segment) : SegmentEnumerable.Empty;

        public SegmentEnumerable Get(Type segment) => TryGetSegment(segment, out var data) ? new SegmentEnumerable(data) : SegmentEnumerable.Empty;

        public bool Clear()
        {
            var cleared = _free.count > 0 || _frozen.count > 0 || _data.count > 0 || _segments.count > 0;
            for (var i = 0; i < _segments.count; i++) if (_segments.items[i] is SegmentData segment) cleared |= Clear(segment);
            _free.Clear();
            _frozen.Clear();
            _data.Clear();
            _segments.Clear();
            return cleared;
        }

        public bool Clear(Type segment) => TryGetSegment(segment, out var data) && Clear(data);

        public bool Clear<T>() where T : struct, ISegment => TryGetSegment<T>(out var segment) && Clear(segment);

        public void Resolve()
        {
            while (_frozen.count > 0) _free.Push(_frozen.Pop());
            for (int i = 0; i < _segments.count; i++)
                if (_segments.items[i] is SegmentData segment) Resolve(segment);
        }

        public bool TryData(Entity entity, out Data data)
        {
            if (entity.Index < _data.count)
            {
                data = _data.items[entity.Index];
                return data.Allocated && data.Generation == entity.Generation;
            }

            data = default;
            return false;
        }

        public bool TryMask(Entity entity, out BitMask mask)
        {
            if (TryData(entity, out var data))
            {
                mask = data.Mask;
                return true;
            }

            mask = default;
            return false;
        }

        public bool TrySegment(Entity entity, out Type segment)
        {
            if (TryData(entity, out var data) && IndexUtility<ISegment>.TryGetType(data.Segment.local, out segment)) return true;
            segment = default;
            return false;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        Entity Create((int global, int local) segment, Action<Messages, Entity> onCreate)
        {
            var reserved = ReserveIndex();
            ref var data = ref _data.items[reserved];
            var entity = new Entity(reserved, ++data.Generation);

            var segmentData = GetSegment(segment.local);
            data.Segment = segment;
            data.Allocated = true;
            data.Index = ReserveIndex(segmentData);
            data.Mask = data.Mask ?? new BitMask();
            data.Mask.Add(segment.global);
            segmentData.Entities.items[data.Index] = entity;
            onCreate(_messages, entity);
            return entity;
        }

        void Destroy(Entity entity, Data data, Action<Messages, Entity> onPreDestroy, Action<Messages, Entity> onPostDestroy)
        {
            onPreDestroy(_messages, entity);
            Freeze(_segments.items[data.Segment.local], data.Index);
            data.Allocated = false;
            data.Mask.Clear();
            _data.items[entity.Index] = data;
            _frozen.Push(entity.Index);
            onPostDestroy(_messages, entity);
        }

        bool Clear(SegmentData segment)
        {
            var cleared = segment.Entities.count > 0 || segment.Free.count > 0 || segment.Frozen.count > 0;
            foreach (var entity in segment) Destroy(entity);

            segment.Entities.Clear();
            segment.Free.Clear();
            segment.Frozen.Clear();
            return cleared;
        }

        bool TryGetSegment<T>(out SegmentData segment) where T : struct, ISegment => TryGetSegment(IndexUtility<ISegment>.Cache<T>.Index.local, out segment);

        bool TryGetSegment(Type type, out SegmentData segment)
        {
            if (IndexUtility<ISegment>.TryGetIndex(type, out var index) && TryGetSegment(index.local, out segment)) return true;
            segment = default;
            return false;
        }

        bool TryGetSegment(int local, out SegmentData segment)
        {
            segment = local < _segments.count ? _segments.items[local] : default;
            return segment != null;
        }

        SegmentData GetSegment(int local)
        {
            if (TryGetSegment(local, out var segment)) return segment;
            _segments.Set(segment = new SegmentData(), local);
            return segment;
        }

        int ReserveIndex()
        {
            // Priotising the increase of the maximum index until it hits the capacity makes sure that all available indices are used.
            var index = _data.count < _data.items.Length || _free.count == 0 ? _data.count++ : _free.Pop();
            _data.Ensure();
            return index;
        }

        static int ReserveIndex(SegmentData segment)
        {
            // Priotising the increase of the maximum index until it hits the capacity makes sure that all available indices are used.
            var index = segment.Entities.count < segment.Entities.items.Length || segment.Free.count == 0 ? segment.Entities.count++ : segment.Free.Pop();
            segment.Entities.Ensure();
            return index;
        }

        static void Freeze(SegmentData segment, int index)
        {
            segment.Entities.items[index] = default;
            segment.Frozen.Push(index);
        }

        static void Resolve(SegmentData segment)
        {
            while (segment.Frozen.count > 0) segment.Free.Push(segment.Frozen.Pop());
        }
    }
}