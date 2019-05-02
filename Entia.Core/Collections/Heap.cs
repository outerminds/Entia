using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Core
{
    public sealed class Heap<T> : IEnumerable<Heap<T>.Enumerator, T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public ref readonly T Current => ref _heap._items[_index];
            T IEnumerator<T>.Current => _heap._items[_index];
            object IEnumerator.Current => Current;

            Heap<T> _heap;
            int _index;

            public Enumerator(Heap<T> heap)
            {
                _heap = heap;
                _index = -1;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext() => ++_index < _heap.Count;
            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset() => _index = -1;
            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose() => _heap = null;
        }

        public int Count { get; private set; }
        public int Capacity => _items.Length;

        readonly IComparer<T> _comparer;
        T[] _items;

        public Heap(IComparer<T> comparer = null, int capacity = 2)
        {
            _comparer = comparer ?? Comparer<T>.Default;
            _items = new T[capacity];
        }

        public T Pop()
        {
            var top = _items[0];
            _items[0] = _items[Count - 1];
            Count--;
            BubbleDown(0);
            return top;
        }

        public void Push(T item)
        {
            var index = Count++;
            ArrayUtility.Ensure(ref _items, Count);
            _items[index] = item;
            BubbleUp(index);
        }

        public ref readonly T Peek() => ref _items[0];

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void BubbleUp(int index)
        {
            if (index >= 0)
            {
                var parentIndex = Parent(index);
                var child = _items[index];
                var parent = _items[parentIndex];
                if (parentIndex >= 0 && _comparer.Compare(child, parent) < 0)
                {
                    _items[parentIndex] = child;
                    _items[index] = parent;
                    BubbleUp(parentIndex);
                }
            }
        }

        void BubbleDown(int index)
        {
            if (!IsLeaf(index))
            {
                var childIndex = LeftChild(index);
                if (childIndex < Count)
                {
                    var child = _items[childIndex];
                    var rightChild = _items[childIndex + 1];
                    if (_comparer.Compare(child, rightChild) > 0)
                    {
                        childIndex++;
                    }
                    child = _items[childIndex];
                    var parent = _items[index];
                    if (_comparer.Compare(parent, child) > 0)
                    {
                        _items[index] = child;
                        _items[childIndex] = parent;
                        BubbleDown(childIndex);
                    }
                }
            }
        }

        int LeftChild(int index) => (2 * index) + 1;
        int RightChild(int index) => (2 * index) + 2;
        int Parent(int index) => (index - 1) / 2;
        bool IsLeaf(int index) => (index >= Count / 2) && (index < Count);
    }
}