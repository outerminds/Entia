using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Entia.Core
{
    public static class Concurrent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Mutate<T>(ref T location, in T value) where T : class
        {
            T initial, comparand, mutated;
            do
            {
                comparand = location;
                mutated = value;
                initial = Interlocked.CompareExchange(ref location, mutated, comparand);
            }
            while (initial != comparand);
            return mutated;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Mutate<T>(ref T location, Func<T, T> mutate) where T : class
        {
            T initial, comparand, mutated;
            do
            {
                comparand = location;
                mutated = mutate(comparand);
                initial = Interlocked.CompareExchange(ref location, mutated, comparand);
            }
            while (initial != comparand);
            return mutated;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Mutate<T, TState>(ref T location, in TState state, Func<T, TState, T> mutate) where T : class
        {
            T initial, comparand, mutated;
            do
            {
                comparand = location;
                mutated = mutate(comparand, state);
                initial = Interlocked.CompareExchange(ref location, mutated, comparand);
            }
            while (initial != comparand);
            return mutated;
        }
    }

    public sealed class Concurrent<T>
    {
        public readonly struct ReadDisposable : IDisposable
        {
            public ref readonly T Value
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _concurrent._value;
            }

            readonly Concurrent<T> _concurrent;
            readonly Action<Concurrent<T>> _dispose;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadDisposable(Concurrent<T> concurrent, Action<Concurrent<T>> dispose)
            {
                _concurrent = concurrent;
                _dispose = dispose;
            }

            /// <inheritdoc cref="IDisposable.Dispose"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => _dispose(_concurrent);
        }

        public readonly struct WriteDisposable : IDisposable
        {
            public ref T Value
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _concurrent._value;
            }

            readonly Concurrent<T> _concurrent;
            readonly Action<Concurrent<T>> _dispose;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public WriteDisposable(Concurrent<T> concurrent, Action<Concurrent<T>> dispose)
            {
                _concurrent = concurrent;
                _dispose = dispose;
            }

            /// <inheritdoc cref="IDisposable.Dispose"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => _dispose(_concurrent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Concurrent<T>(T value) => new Concurrent<T>(value);

        T _value;
        readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Concurrent(T value) { _value = value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WriteDisposable Write()
        {
            _lock.EnterWriteLock();
            return new WriteDisposable(this, concurrent => concurrent._lock.ExitWriteLock());
        }

        public TValue Read<TValue>(InFunc<T, TValue> read)
        {
            using (var value = Read()) return read(value.Value);
        }

        public TValue Read<TValue>(Func<T, TValue> read)
        {
            using (var value = Read()) return read(value.Value);
        }

        public TValue Read<TValue, TState>(in TState state, InFunc<T, TState, TValue> read)
        {
            using (var value = Read()) return read(value.Value, state);
        }

        public TValue Read<TValue, TState>(in TState state, Func<T, TState, TValue> read)
        {
            using (var value = Read()) return read(value.Value, state);
        }

        public void Write(Action<T> write)
        {
            using (var value = Write()) write(value.Value);
        }

        public void Write(RefAction<T> write)
        {
            using (var value = Write()) write(ref value.Value);
        }

        public void Write<TState>(in TState state, RefInAction<T, TState> write)
        {
            using (var value = Write()) write(ref value.Value, state);
        }

        public void Write<TState>(in TState state, Action<T, TState> write)
        {
            using (var value = Write()) write(value.Value, state);
        }

        public TValue Write<TValue>(Func<T, TValue> write)
        {
            using (var value = Write()) return write(value.Value);
        }

        public TValue Write<TValue, TState>(in TState state, RefInFunc<T, TState, TValue> write)
        {
            using (var value = Write()) return write(ref value.Value, state);
        }

        public TValue Write<TValue, TState>(in TState state, Func<T, TState, TValue> write)
        {
            using (var value = Write()) return write(value.Value, state);
        }
    }
}