using System;
using Entia.Core;

namespace Entia.Core
{
    public sealed class Pool<T>
    {
        public readonly object Lock = new object();

        readonly Func<T> _create;
        readonly Action<T> _initialize;
        readonly Action<T> _dispose;
        (T[] items, int count) _items = (new T[4], 0);

        public Pool(Func<T> create, Action<T> initialize = null, Action<T> dispose = null)
        {
            _create = create;
            _initialize = initialize ?? (_ => { });
            _dispose = dispose ?? (_ => { });
        }

        public T Take()
        {
            T item;
            bool success;
            lock (Lock) { success = _items.TryPop(out item); }
            if (!success) item = _create();
            _initialize(item);
            return item;
        }

        public void Put(T item)
        {
            _dispose(item);
            lock (Lock) _items.Push(item);
        }
    }
}
