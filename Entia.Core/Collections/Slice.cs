using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Entia.Core.Documentation;

namespace Entia.Core
{
    [ThreadSafe]
    public readonly struct Slice<T> : IEnumerable<T>
    {
        [ThreadSafe]
        public readonly struct Read : IEnumerable<T>
        {
            public struct Enumerator : IEnumerator<T>
            {
                /// <inheritdoc cref="IEnumerator{T}.Current"/>
                public ref readonly T Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref _slice[_index];
                }
                T IEnumerator<T>.Current => Current;
                object IEnumerator.Current => Current;

                Read _slice;
                int _index;

                public Enumerator(Read slice)
                {
                    _slice = slice;
                    _index = -1;
                }

                /// <inheritdoc cref="IEnumerator.MoveNext"/>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext() => ++_index < _slice.Count;
                /// <inheritdoc cref="IDisposable.Dispose"/>
                public void Dispose() => _slice = default;
                /// <inheritdoc cref="IEnumerator.Reset"/>
                public void Reset() => _index = -1;
            }

            public ref readonly T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _array[_offset + index];
            }
            public int Count { get; }

            readonly T[] _array;
            readonly int _offset;

            public Read(T[] array, int index, int count)
            {
                _array = array;
                _offset = index;
                Count = count;
            }

            public T[] ToArray()
            {
                var current = new T[Count];
                Array.Copy(_array, _offset, current, 0, Count);
                return current;
            }

            /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
            public Enumerator GetEnumerator() => new Enumerator(this);
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct Enumerator : IEnumerator<T>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _slice[_index];
            }
            T IEnumerator<T>.Current => Current;
            object IEnumerator.Current => Current;

            Slice<T> _slice;
            int _index;

            public Enumerator(Slice<T> slice)
            {
                _slice = slice;
                _index = -1;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _slice.Count;
            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose() => _slice = default;
            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset() => _index = -1;
        }

        public static implicit operator Read(Slice<T> slice) => new Read(slice._array, slice._offset, slice.Count);

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[_offset + index];
        }
        public int Count { get; }

        readonly T[] _array;
        readonly int _offset;

        public Slice(T[] array, int index, int count)
        {
            _array = array;
            _offset = index;
            Count = count;
        }

        public T[] ToArray()
        {
            var current = new T[Count];
            Array.Copy(_array, _offset, current, 0, Count);
            return current;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
