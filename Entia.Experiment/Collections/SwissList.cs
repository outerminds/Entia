using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Entia.Core
{
    public sealed class SwissList<T> : IEnumerable<SwissList<T>.Enumerator, T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public ref T Current => ref _list._items.items[_index];
            T IEnumerator<T>.Current => Current;
            object IEnumerator.Current => Current;

            readonly SwissList<T> _list;
            int _index;

            public Enumerator(SwissList<T> list)
            {
                _list = list;
                _index = -1;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_index < _list._items.count)
                    if (_list._allocated[_index]) return true;

                return false;
            }

            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset() => _index = -1;
            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose() => this = default;
        }

        public int Count => _items.count - _free.count;
        public int Capacity => _items.items.Length;
        public int Maximum => _items.count;
        public int Next => _free.count > 0 ? _free.Peek() : _items.count;
        public ref T this[int index]
        {
            get
            {
                if (Has(index)) return ref _items.items[index];
                throw new IndexOutOfRangeException();
            }
        }

        (int[] items, int count) _free = (new int[8], 0);
        (T[] items, int count) _items;
        bool[] _allocated;

        public SwissList(int capacity = 4)
        {
            _items = (new T[capacity], 0);
            _allocated = new bool[capacity];
        }

        public SwissList(IEnumerable<T> items)
        {
            var array = items.ToArray();
            _items = (array, array.Length);
            _allocated = new bool[array.Length];
            _allocated.Fill(true);
        }

        public bool Has(int index) => index < Maximum && _allocated[index];

        public bool TryGet(int index, out T item)
        {
            if (Has(index))
            {
                item = _items.items[index];
                return true;
            }

            item = default;
            return false;
        }

        public bool TrySet(int index, in T item)
        {
            if (Has(index))
            {
                _items.items[index] = item;
                return true;
            }

            return false;
        }

        public int Add(in T item)
        {
            var index = ReserveIndex();
            _items.items[index] = item;
            _allocated[index] = true;
            return index;
        }

        public bool Remove(int index)
        {
            if (Has(index))
            {
                _allocated[index] = false;
                _free.Push(index);
                return true;
            }

            return false;
        }

        public bool Clear()
        {
            _allocated.Clear();
            return _items.Clear() | _free.Clear();
        }

        public T[] ToArray()
        {
            var array = new T[Count];

            for (int i = 0, j = 0; i < Maximum; i++)
                if (_allocated[i]) array[j++] = _items.items[i];

            return array;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        int ReserveIndex()
        {
            if (_free.count > 0) return _free.Pop();

            var index = _items.count++;
            Ensure(_items.count);
            return index;
        }

        void Ensure(int size)
        {
            ArrayUtility.Ensure(ref _items.items, size);
            ArrayUtility.Ensure(ref _allocated, size);
        }
    }
}
