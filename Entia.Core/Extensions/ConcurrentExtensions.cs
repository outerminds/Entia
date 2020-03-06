using System;
using System.Collections;
using System.Collections.Generic;
using Entia.Core.Documentation;

namespace Entia.Core
{
    [ThreadSafe]
    public static class ConcurrentExtensions
    {
        public static int ReadCount<T>(this Concurrent<T> concurrent) where T : ICollection
        {
            using (var read = concurrent.Read()) return read.Value.Count;
        }

        public static T ReadAt<T>(this Concurrent<List<T>> concurrent, int index)
        {
            using (var read = concurrent.Read()) return read.Value[index];
        }

        public static bool TryReadAt<T>(this Concurrent<List<T>> concurrent, int index, out T value)
        {
            using var read = concurrent.Read();
            if (index < read.Value.Count)
            {
                value = read.Value[index];
                return true;
            }

            value = default;
            return false;
        }

        public static T[] ReadToArray<T>(this Concurrent<List<T>> concurrent)
        {
            using (var read = concurrent.Read()) return read.Value.ToArray();
        }

        public static T ReadAt<T>(this Concurrent<T[]> concurrent, int index)
        {
            using (var read = concurrent.Read()) return read.Value[index];
        }

        public static bool TryReadAt<T>(this Concurrent<T[]> concurrent, int index, out T value)
        {
            using var read = concurrent.Read();
            if (index < read.Value.Length)
            {
                value = read.Value[index];
                return true;
            }

            value = default;
            return false;
        }

        public static int ReadCount<T>(this Concurrent<(T[] items, int count)> concurrent)
        {
            using (var read = concurrent.Read()) return read.Value.count;
        }

        public static T[] ReadToArray<T>(this Concurrent<(T[] items, int count)> concurrent)
        {
            using (var read = concurrent.Read()) return read.Value.ToArray();
        }

        public static ref readonly T ReadAt<T>(this Concurrent<(T[] items, int count)> concurrent, int index)
        {
            using (var read = concurrent.Read()) return ref read.Value.items[index];
        }

        public static bool TryReadAt<T>(this Concurrent<(T[] items, int count)> concurrent, int index, out T value)
        {
            using var read = concurrent.Read();
            if (index < read.Value.count)
            {
                value = read.Value.items[index];
                return true;
            }

            value = default;
            return false;
        }

        public static TValue ReadValue<TKey, TValue>(this Concurrent<Dictionary<TKey, TValue>> concurrent, TKey key)
        {
            using (var read = concurrent.Read()) return read.Value[key];
        }

        public static bool TryReadValue<TKey, TValue>(this Concurrent<Dictionary<TKey, TValue>> concurrent, TKey key, out TValue value)
        {
            using (var read = concurrent.Read()) return read.Value.TryGetValue(key, out value);
        }

        public static TValue ReadValueOrWrite<TKey, TValue, TState>(this Concurrent<Dictionary<TKey, TValue>> concurrent, TKey key, TState state, Func<TState, (TKey key, TValue value)> provide)
        {
            using var read = concurrent.Read(true);
            if (read.Value.TryGetValue(key, out var types)) return types;
            using var write = concurrent.Write();
            if (write.Value.TryGetValue(key, out types)) return types;
            var pair = provide(state);
            return write.Value[pair.key] = pair.value;
        }

        public static TValue ReadValueOrWrite<TBase, TValue, TState>(this Concurrent<TypeMap<TBase, TValue>> concurrent, int index, TState state, Func<TState, TValue> provide, Action<TState> onWrite = null)
        {
            TValue value;
            using (var read = concurrent.Read(true))
            {
                if (read.Value.TryGet(index, out value)) return value;
                using var write = concurrent.Write();
                if (write.Value.TryGet(index, out value)) return value;
                value = provide(state);
                write.Value.Set(index, value);
            }

            onWrite?.Invoke(state);
            return value;
        }

        public static TValue ReadValueOrWrite<TBase, TValue, TState>(this Concurrent<TypeMap<TBase, TValue>> concurrent, Type type, TState state, Func<TState, TValue> provide, Action<TState> onWrite = null, bool super = false, bool sub = false)
        {
            TValue value;
            using (var read = concurrent.Read(true))
            {
                if (read.Value.TryGet(type, out value, super, sub)) return value;
                using var write = concurrent.Write();
                if (write.Value.TryGet(type, out value, super, sub)) return value;
                value = provide(state);
                write.Value.Set(type, value);
            }

            onWrite?.Invoke(state);
            return value;
        }
    }
}