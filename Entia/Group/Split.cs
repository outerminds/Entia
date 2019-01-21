using Entia.Queryables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Entia.Modules.Group
{
    /// <summary>
    /// Represents a slice of a give size over an array of segments.
    /// A split may span over multiple segments.
    /// </summary>
    /// <typeparam name="T">The query type.</typeparam>
    public readonly struct Split<T> : IEnumerable<T> where T : struct, IQueryable
    {
        /// <summary>
        /// An enumerator that enumerates over the split's items.
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public ref readonly T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _items[_index];
            }
            T IEnumerator<T>.Current => Current;
            object IEnumerator.Current => Current;

            Split<T> _split;
            int _segment;
            int _index;
            int _total;
            int _count;
            T[] _items;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> struct.
            /// </summary>
            /// <param name="split">The split.</param>
            public Enumerator(in Split<T> split)
            {
                _split = split;
                _segment = split._segment;
                _index = split._index - 1;
                _total = split.Count;

                var segment = split._segments[_segment];
                _items = segment.Items;
                _count = segment.Count;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                // NOTE: check '_total' after the '_index' such that it is not decremented if the current segment is exhausted
                if (++_index < _count && --_total >= 0) return true;
                while (++_segment < _split._segments.Length)
                {
                    var segment = _split._segments[_segment];
                    _count = segment.Count;
                    if (_count > 0)
                    {
                        _items = segment.Items;
                        _index = 0;
                        return --_total >= 0;
                    }
                }

                return false;
            }

            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset()
            {
                _segment = _split._segment;
                _index = _split._index;
                _total = _split.Count;

                var segment = _split._segments[_segment];
                _items = segment.Items;
                _count = segment.Count;
            }

            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose()
            {
                _split = default;
                _items = default;
            }
        }

        /// <summary>
        /// The entity count.
        /// </summary>
        public readonly int Count;

        readonly Segment<T>[] _segments;
        readonly int _segment;
        readonly int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Split{T}"/> struct.
        /// </summary>
        /// <param name="segments">The segments.</param>
        /// <param name="segment">The segment.</param>
        /// <param name="index">The index.</param>
        /// <param name="count">The count.</param>
        public Split(Segment<T>[] segments, int segment, int index, int count)
        {
            _segments = segments;
            _segment = segment;
            _index = index;
            Count = count;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}