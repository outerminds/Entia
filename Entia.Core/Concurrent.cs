using System;
using System.Threading;

namespace Entia.Core
{
    public sealed class Concurrent<T>
    {
        public readonly struct ReadDisposable : IDisposable
        {
            public ref readonly T Value => ref _concurrent._value;

            readonly Concurrent<T> _concurrent;
            readonly Action<Concurrent<T>> _dispose;

            public ReadDisposable(Concurrent<T> concurrent, Action<Concurrent<T>> dispose)
            {
                _concurrent = concurrent;
                _dispose = dispose;
            }

            public void Dispose() => _dispose(_concurrent);
        }

        public readonly struct WriteDisposable : IDisposable
        {
            public ref T Value => ref _concurrent._value;

            readonly Concurrent<T> _concurrent;
            readonly Action<Concurrent<T>> _dispose;

            public WriteDisposable(Concurrent<T> concurrent, Action<Concurrent<T>> dispose)
            {
                _concurrent = concurrent;
                _dispose = dispose;
            }

            public void Dispose() => _dispose(_concurrent);
        }

        public static implicit operator Concurrent<T>(T value) => new Concurrent<T>(value);

        T _value;
        readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public Concurrent(T value) { _value = value; }

        public ReadDisposable Read(bool upgradeable = false)
        {
            if (upgradeable)
            {
                _lock.EnterUpgradeableReadLock();
                return new ReadDisposable(this, concurrent => concurrent._lock.ExitUpgradeableReadLock());
            }
            else
            {
                _lock.EnterReadLock();
                return new ReadDisposable(this, concurrent => concurrent._lock.ExitReadLock());
            }
        }

        public WriteDisposable Write()
        {
            _lock.EnterWriteLock();
            return new WriteDisposable(this, concurrent => concurrent._lock.ExitWriteLock());
        }

        public TValue Read<TValue>(InFunc<T, TValue> read)
        {
            using (var value = Read()) return read(value.Value);
        }

        public TValue Read<TValue, TState>(in TState state, InFunc<T, TState, TValue> read)
        {
            using (var value = Read()) return read(value.Value, state);
        }

        public void Write(RefAction<T> write)
        {
            using (var value = Write()) write(ref value.Value);
        }

        public void Write<TState>(in TState state, RefInAction<T, TState> write)
        {
            using (var value = Write()) write(ref value.Value, state);
        }

        public TValue Write<TValue, TState>(in TState state, RefInFunc<T, TState, TValue> write)
        {
            using (var value = Write()) return write(ref value.Value, state);
        }
    }
}