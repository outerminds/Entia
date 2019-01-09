using System;

namespace Entia.Core
{
    public readonly struct Disposable : IDisposable
    {
        public static readonly Disposable Empty = new Disposable(() => { });

        readonly Action _dispose;

        public Disposable(Action dispose) { _dispose = dispose; }

        public void Dispose() => _dispose();
    }

    public readonly struct Disposable<T> : IDisposable
    {
        public static readonly Disposable<T> Empty = new Disposable<T>(default, _ => { });

        readonly T _state;
        readonly Action<T> _dispose;

        public Disposable(in T state, Action<T> dispose)
        {
            _state = state;
            _dispose = dispose;
        }

        public void Dispose() => _dispose(_state);
    }
}
