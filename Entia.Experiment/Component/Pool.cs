using System;
using Entia.Core;

namespace Entia.Core
{
    public sealed class Pool<T>
    {
        public int Count => _items.ReadCount();

        readonly Func<T> _create;
        readonly Action<T> _initialize;
        readonly Action<T> _dispose;
        readonly Concurrent<(T[] items, int count)> _items = (new T[0], 0);

        public Pool(Func<T> create, Action<T> initialize = null, Action<T> dispose = null)
        {
            _create = create;
            _initialize = initialize ?? (_ => { });
            _dispose = dispose ?? (_ => { });
        }

        public T Take()
        {
            using (var read = _items.Read(true))
            {
                T item;
                if (read.Value.count == 0) item = _create();
                else using (var write = _items.Write()) item = write.Value.Pop();

                _initialize(item);
                return item;
            }
        }

        public void Put(T item)
        {
            _dispose(item);
            using (var write = _items.Write()) write.Value.Push(item);
        }
    }
}
