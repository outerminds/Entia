using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Core
{
    public sealed class Buffer<T> : IEnumerable<Buffer<T>.Enumerator, T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public ref T Current => ref _chunk[_adjusted];
            T IEnumerator<T>.Current => Current;
            object IEnumerator.Current => Current;

            Buffer<T> _buffer;
            int _index;
            T[] _chunk;
            int _adjusted;

            public Enumerator(Buffer<T> buffer)
            {
                _buffer = buffer;
                _index = -1;
                _adjusted = -1;
                _chunk = buffer._items;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext()
            {
                while (++_index < _buffer.Maximum)
                {
                    ++_adjusted;
                    if (_buffer._allocated[_index])
                        return _adjusted < _chunk.Length || _buffer.TryGet(_index, out _chunk, out _adjusted);
                }

                return false;
            }

            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset()
            {
                _index = -1;
                _adjusted = -1;
                _chunk = _buffer._items;
            }

            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose()
            {
                _buffer = null;
                _chunk = null;
            }
        }

        public const int Overflow = 8;

        public int Capacity => _allocated.Length;
        public int Count { get; private set; }
        public int Maximum { get; private set; }
        public ref T this[int index]
        {
            get
            {
                if (TryGet(index, out var buffer, out var adjusted)) return ref buffer[adjusted];
                throw new IndexOutOfRangeException();
            }
        }

        readonly List<T[]> _overflows = new List<T[]>();
        T[] _items;
        bool[] _allocated;

        public Buffer(int capacity = Overflow)
        {
            _items = new T[capacity];
            _allocated = new bool[capacity];
        }

        public bool Has(int index) => index < _allocated.Length && _allocated[index];

        public bool TryGet(int index, out T[] chunk, out int adjusted)
        {
            if (index < _items.Length)
            {
                chunk = _items;
                adjusted = index;
                return true;
            }

            var overflow = (index - _items.Length) / Overflow;
            if (overflow < _overflows.Count)
            {
                chunk = _overflows[overflow];
                adjusted = (index - _items.Length) % Overflow;
                return true;
            }

            chunk = default;
            adjusted = default;
            return false;
        }

        public bool Set(in T value, int index)
        {
            var (buffer, adjusted) = Get(index);
            buffer[adjusted] = value;

            ArrayUtility.Ensure(ref _allocated, index + 1);
            if (_allocated[index].Change(true))
            {
                Maximum = Math.Max(index + 1, Maximum);
                Count++;
                return true;
            }

            return false;
        }

        public bool Remove(int index)
        {
            if (_allocated[index].Change(false))
            {
                Count--;
                return true;
            }

            return false;
        }

        public (T[] chunk, int adjusted) Get(int index)
        {
            if (index < _items.Length) return (_items, index);

            var adjusted = (index - _items.Length) % Overflow;
            var overflow = (index - _items.Length) / Overflow;

            while (overflow >= _overflows.Count)
                _overflows.Add(new T[Overflow]);

            return (_overflows[overflow], adjusted);
        }

        public bool Clear()
        {
            var cleared = Count > 0 || Maximum > 0 || _overflows.Count > 0;
            Count = 0;
            Maximum = 0;
            _items.Clear();
            _allocated.Clear();
            _overflows.Clear();
            return cleared;
        }

        public bool Compact()
        {
            var count = _items.Length;
            if (ArrayUtility.Ensure(ref _items, _items.Length + _overflows.Count * Overflow))
            {
                for (var i = 0; i < _overflows.Count; i++)
                {
                    var overflow = _overflows[i];
                    Array.Copy(overflow, 0, _items, count + i * Overflow, overflow.Length);
                }

                _overflows.Clear();
                return true;
            }

            return false;
        }

        public bool Defragment()
        {
            void Move(int source, int target)
            {
                _items[target] = _items[source];
                _allocated[source] = false;
                _allocated[target] = true;
            }

            var moved = false;
            var index = _allocated.Length;
            var last = -1;
            while (--index >= 0)
            {
                if (_allocated[index]) last = Math.Max(last, index);
                else if (last > 0)
                {
                    Move(last, index);
                    Maximum = last;
                    index = last;
                    last = -1;
                    moved = true;
                }
            }

            return moved;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
