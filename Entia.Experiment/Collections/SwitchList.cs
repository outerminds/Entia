using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Entia.Core
{
    public sealed class SwitchList<T> : IEnumerable<SwitchList<T>.Enumerator, T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public ref T Current => ref _list._items.items[_index];
            T IEnumerator<T>.Current => Current;
            object IEnumerator.Current => Current;

            SwitchList<T> _list;
            int _index;

            public Enumerator(SwitchList<T> list)
            {
                _list = list;
                _index = -1;
            }

            public bool MoveNext() => ++_index < _list._items.count;
            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset() => _index = -1;
            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose() => _list = null;
        }

        public int Count => _items.count;
        public int Capacity => _items.items.Length;

        public ref T this[int index] => ref _items.items[index];

        (T[] items, int count) _items;

        public SwitchList(int capacity = 4) { _items = (new T[capacity], 0); }

        public SwitchList(IEnumerable<T> items)
        {
            var array = items.ToArray();
            _items = (array, array.Length);
        }

        public bool TryGet(int index, out T item)
        {
            if (index < Count)
            {
                item = _items.items[index];
                return true;
            }

            item = default;
            return false;
        }

        public bool TrySet(int index, in T item)
        {
            if (index < Count)
            {
                _items.items[index] = item;
                return true;
            }

            return false;
        }

        public int Add(T item)
        {
            var index = _items.count;
            _items.Set(index, item);
            return index;
        }

        public bool Remove(int index)
        {
            if (index < _items.count)
            {
                if (--_items.count > 0) _items.items[index] = _items.items[_items.count];
                return true;
            }

            return false;
        }

        public bool Clear() => _items.Clear();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
