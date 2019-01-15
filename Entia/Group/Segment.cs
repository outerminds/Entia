using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Entia.Queryables;

namespace Entia.Modules.Group
{
    public readonly struct Segment<T> : IEnumerable<(Entity entity, T item)> where T : struct, IQueryable
    {
        public struct Enumerator : IEnumerator<(Entity entity, T item)>
        {
            public (Entity entity, T item) Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _segment[_index];
            }
            object IEnumerator.Current => Current;

            Segment<T> _segment;
            int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(Segment<T> segment)
            {
                _segment = segment;
                _index = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _segment.Count;
            public void Reset() => _index = -1;
            public void Dispose() => _segment = default;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _segment.Entities.count;
        }
        public (Entity entity, T item) this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Entities[index], Items[index]);
        }
        public Component.Metadata[] Types
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _segment.Types.data;
        }
        public Entity[] Entities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _segment.Entities.items;
        }

        public readonly T[] Items;

        readonly Component.Segment _segment;

        public Segment(Component.Segment segment, T[] items)
        {
            _segment = segment;
            Items = items;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<(Entity entity, T item)> IEnumerable<(Entity entity, T item)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}