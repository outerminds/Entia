using System;

namespace Entia.Core
{
    public sealed class Pool<T> where T : class
    {
        public readonly struct Disposable : IDisposable
        {
            public readonly T Instance;
            readonly Pool<T> _pool;

            public Disposable(Pool<T> pool)
            {
                _pool = pool;
                Instance = pool.Take();
            }

            public void Dispose() => _pool.Put(Instance);
        }

        readonly Func<T> _create;
        readonly Action<T> _initialize;
        readonly Action<T> _dispose;
        (T[] items, int count) _items;

        public Pool(Func<T> create, Action<T> initialize = null, Action<T> dispose = null, int capacity = 4)
        {
            _create = create;
            _initialize = initialize ?? (_ => { });
            _dispose = dispose ?? (_ => { });
            _items = (new T[capacity], 0);
        }

        public T Take()
        {
            var instance = _items.TryPop(out var item) ? item : _create();
            _initialize(instance);
            return instance;
        }

        public void Put(T instance)
        {
            _dispose(instance);
            _items.Push(instance);
        }

        public Disposable Use() => new Disposable(this);
    }
}