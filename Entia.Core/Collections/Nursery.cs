using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Entia.Core.Documentation;

namespace Entia.Core
{
    public struct Nursery<T> where T : class
    {
        readonly Func<T> _create;
        readonly Action<T> _initialize;
        readonly int _chunk;

        int _next;
        T[][] _chunks;

        public Nursery(Func<T> create, Action<T> initialize = null, int chunk = 8)
        {
            _create = create;
            _initialize = initialize ?? (_ => { });
            _chunk = chunk;
            _next = -1;
            _chunks = new T[][] { new T[chunk] };
        }

        [ThreadSafe]
        public T Allocate()
        {
            var index = Interlocked.Increment(ref _next);
            var chunk = Chunk(index, out index);
            var item = chunk[index] ?? (chunk[index] = _create());
            _initialize(item);
            return item;
        }

        public bool Free() => _next.Change(-1);

        [ThreadSafe]
        T[] Chunk(int index, out int adjusted)
        {
            var chunk = index / _chunk;
            adjusted = index % _chunk;
            if (chunk >= _chunks.Length)
            {
                // NOTE: the lock only tries to prevent multiple resize/assign of the array;
                // it doesn't matter if another thread access different outer chunks arrays since the inner chunk arrays never move
                lock (_chunks) { if (chunk >= _chunks.Length) ArrayUtility.Add(ref _chunks, new T[_chunk]); }
            }
            return _chunks[chunk];
        }
    }
}
