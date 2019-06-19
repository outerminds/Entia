using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Entia.Core.Documentation;

namespace Entia.Core
{
    [ThreadSafe]
    public readonly struct Slice<T> : IEnumerable<Slice<T>.Enumerator, T>
    {
        [ThreadSafe]
        public readonly struct Read : IEnumerable<Read.Enumerator, T>
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

                public Enumerator(Read slice, int index = -1)
                {
                    _slice = slice;
                    _index = index;
                }

                /// <inheritdoc cref="IEnumerator.MoveNext"/>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext() => ++_index < _slice.Count;
                /// <inheritdoc cref="IDisposable.Dispose"/>
                public void Dispose() => _slice = default;
                /// <inheritdoc cref="IEnumerator.Reset"/>
                public void Reset() => _index = -1;
            }

            public static implicit operator Read(T[] array) => new Read(array, 0, array.Length);

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
            public static implicit operator Read.Enumerator(in Enumerator enumerator) => new Read.Enumerator(enumerator._slice, enumerator._index);

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

            public Enumerator(Slice<T> slice, int index = -1)
            {
                _slice = slice;
                _index = index;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _slice.Count;
            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose() => _slice = default;
            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset() => _index = -1;
        }

        public static implicit operator Slice<T>(T[] array) => new Slice<T>(array, 0, array.Length);
        public static implicit operator Read(in Slice<T> slice) => new Read(slice._array, slice._offset, slice.Count);

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
