using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Entia.Core
{
    public struct ConcurrentPool<T> where T : class
    {
        readonly Func<T> _create;
        readonly Action<T> _initialize;
        readonly int _chunk;

        int _next;
        T[][] _chunks;

        public ConcurrentPool(Func<T> create, Action<T> initialize = null, int chunk = 8)
        {
            _create = create;
            _initialize = initialize ?? (_ => { });
            _chunk = chunk;
            _next = -1;
            _chunks = new T[][] { new T[chunk] };
        }

        public T Allocate()
        {
            var index = Interlocked.Increment(ref _next);
            var chunk = Chunk(index, out index);
            var item = chunk[index] ?? (chunk[index] = _create());
            _initialize(item);
            return item;
        }

        public bool Free() => _next.Change(-1);

        T[] Chunk(int index, out int adjusted)
        {
            var chunk = index / _chunk;
            adjusted = index % _chunk;
            if (chunk >= _chunks.Length) lock (_chunks) ArrayUtility.Ensure(ref _chunks, chunk + 1);
            return _chunks[chunk] ?? (_chunks[chunk] = new T[_chunk]);
        }
    }
}
