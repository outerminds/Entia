using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Entia.Core
{
    public readonly struct ArrayEnumerable<T> : IEnumerable<T>
    {
        readonly T[] _array;
        readonly int _count;

        public ArrayEnumerable(T[] array, int count)
        {
            _array = array;
            _count = count;
        }

        public ArrayEnumerator<T> GetEnumerator() => new ArrayEnumerator<T>(_array, _count);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct ArrayEnumerator<T> : IEnumerator<T>
    {
        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[_index];
        }
        T IEnumerator<T>.Current => Current;
        object IEnumerator.Current => Current;

        T[] _array;
        int _count;
        int _index;

        public ArrayEnumerator(T[] array, int count)
        {
            _array = array;
            _count = count;
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_index < _count;
        public void Reset() => _index = -1;
        public void Dispose() => _array = null;
    }
}