using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Core
{
    public sealed class TypeMap<TBase, TValue> : IEnumerable<(Type type, TValue value)>
    {
        public struct Enumerator : IEnumerator<(Type type, TValue value)>
        {
            public (Type type, TValue value) Current => (_types.ReadAt(_index), _map._values.items[_index]);
            object IEnumerator.Current => Current;

            TypeMap<TBase, TValue> _map;
            int _index;

            public Enumerator(TypeMap<TBase, TValue> map)
            {
                _map = map;
                _index = -1;
            }
            public bool MoveNext()
            {
                while (++_index < _map._allocated.Length)
                    if (_map._allocated[_index]) return true;

                return false;
            }
            public void Reset() => _index = -1;
            public void Dispose() => _map = null;
        }

        public struct KeyEnumerator : IEnumerator<Type>
        {
            public Type Current => _types.ReadAt(_index);
            object IEnumerator.Current => Current;

            TypeMap<TBase, TValue> _map;
            int _index;

            public KeyEnumerator(TypeMap<TBase, TValue> map)
            {
                _map = map;
                _index = -1;
            }
            public bool MoveNext()
            {
                while (++_index < _map._allocated.Length)
                    if (_map._allocated[_index]) return true;

                return false;
            }
            public void Reset() => _index = -1;
            public void Dispose() => _map = null;
        }

        public readonly struct KeyEnumerable : IEnumerable<Type>
        {
            readonly TypeMap<TBase, TValue> _map;

            public KeyEnumerable(TypeMap<TBase, TValue> map) { _map = map; }
            public KeyEnumerator GetEnumerator() => new KeyEnumerator(_map);
            IEnumerator<Type> IEnumerable<Type>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct ValueEnumerator : IEnumerator<TValue>
        {
            public ref TValue Current => ref _map._values.items[_index];
            TValue IEnumerator<TValue>.Current => Current;
            object IEnumerator.Current => Current;

            TypeMap<TBase, TValue> _map;
            int _index;

            public ValueEnumerator(TypeMap<TBase, TValue> map)
            {
                _map = map;
                _index = -1;
            }
            public bool MoveNext()
            {
                while (++_index < _map._allocated.Length)
                    if (_map._allocated[_index]) return true;

                return false;
            }
            public void Reset() => _index = -1;
            public void Dispose() => _map = null;
        }

        public readonly struct ValueEnumerable : IEnumerable<TValue>
        {
            readonly TypeMap<TBase, TValue> _map;

            public ValueEnumerable(TypeMap<TBase, TValue> map) { _map = map; }
            public ValueEnumerator GetEnumerator() => new ValueEnumerator(_map);
            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public static class Cache<T> where T : TBase
        {
            public static readonly int Index = GetIndex(typeof(T));
            public static readonly int[] Indices = GetIndices(typeof(T));
        }

        static readonly Concurrent<List<Type>> _types = new List<Type>();
        static readonly Concurrent<Dictionary<Type, int>> _typeToIndex = new Dictionary<Type, int>();
        static readonly Concurrent<Dictionary<Type, int[]>> _typeToIndices = new Dictionary<Type, int[]>();

        public static bool TryGetType(int index, out Type type) => _types.TryReadAt(index, out type);
        public static bool TryGetIndex(Type type, out int index) => _typeToIndex.TryReadValue(type, out index);

        public static int GetIndex(Type type) =>
            _typeToIndex.ReadValueOrWrite(type, type, key =>
            {
                using (var types = _types.Write())
                {
                    var index = types.Value.Count;
                    types.Value.Add(key);
                    return (key, index);
                }
            });

        public static int[] GetIndices(Type type) =>
            _typeToIndices.ReadValueOrWrite(type, type, key =>
            {
                var indices = key.GetInterfaces()
                    .Prepend(key)
                    .Concat(key.Bases())
                    .Where(TypeUtility.Is<TBase>)
                    .Select(GetIndex)
                    .ToArray();
                return (key, indices);
            });

        public static bool TryGetIndices(Type type, out int[] indices) => _typeToIndices.TryReadValue(type, out indices);

        public KeyEnumerable Keys => new KeyEnumerable(this);
        public ValueEnumerable Values => new ValueEnumerable(this);
        public ref TValue this[int index]
        {
            get
            {
                if (Has(index)) return ref _values.items[index];
                throw new IndexOutOfRangeException();
            }
        }
        public TValue this[Type type]
        {
            get => TryGet(type, out var value) ? value : throw new IndexOutOfRangeException();
            set => Set(type, value);
        }
        // public int Count { get; private set; }

        (TValue[] items, int count) _values;
        bool[] _allocated;

        public TypeMap(int capacity = 4)
        {
            _values = (new TValue[capacity], 0);
            _allocated = new bool[capacity];
        }

        public TypeMap(params (Type type, TValue value)[] pairs) : this()
        {
            foreach (var pair in pairs) Set(pair.type, pair.value);
        }

        public ref TValue Get<T>(out bool success, bool inherit = false) where T : TBase
        {
            if (inherit) return ref Get(Cache<T>.Indices, out success);
            else return ref Get(Cache<T>.Index, out success);
        }

        public ref TValue Get(Type type, out bool success, bool inherit = false)
        {
            if (inherit) return ref Get(GetIndices(type), out success);
            else return ref Get(GetIndex(type), out success);
        }

        public ref TValue Get(int index, out bool success)
        {
            if (success = Has(index)) return ref _values.items[index];
            return ref Dummy<TValue>.Value;
        }

        public bool TryGet<T>(out TValue value, bool inherit = false) where T : TBase =>
            inherit ? TryGet(Cache<T>.Indices, out value) : TryGet(Cache<T>.Index, out value);

        public bool TryGet(Type type, out TValue value, bool inherit = false)
        {
            if (inherit ? TryGet(GetIndices(type), out value) : TryGet(GetIndex(type), out value)) return true;
            value = default;
            return false;
        }

        public bool TryGet(int index, out TValue value)
        {
            if (Has(index))
            {
                value = _values.items[index];
                return true;
            }

            value = default;
            return false;
        }

        public bool Set<T>(in TValue value) where T : TBase => Set(Cache<T>.Index, value);
        public bool Set<T>(in TValue value, out int index) where T : TBase => Set(index = Cache<T>.Index, value);
        public bool Set(Type type, in TValue value) => Set(GetIndex(type), value);
        public bool Set(Type type, in TValue value, out int index) => Set(index = GetIndex(type), value);
        public bool Set(int index, in TValue value)
        {
            ArrayUtility.Ensure(ref _values.items, index + 1);
            ArrayUtility.Ensure(ref _allocated, index + 1);
            _values.items[index] = value;
            if (_allocated[index].Change(true))
            {
                _values.count++;
                return true;
            }

            return false;
        }

        public bool Has(Type type, bool inherit = false) => inherit ? Has(GetIndices(type)) : Has(GetIndex(type));
        public bool Has<T>(bool inherit = false) where T : TBase => inherit ? Has(Cache<T>.Indices) : Has(Cache<T>.Index);
        public bool Has(int index) => index < _allocated.Length && _allocated[index];

        public bool Remove<T>() where T : TBase => Remove(Cache<T>.Index);
        public bool Remove(Type type) => TryGetIndex(type, out var index) && Remove(index);
        public bool Remove(int index)
        {
            if (Has(index))
            {
                _values.items[index] = default;
                _values.count--;
                _allocated[index] = false;
                return true;
            }

            return false;
        }

        public bool Clear()
        {
            var cleared = _values.count > 0;
            _values.Clear();
            _allocated.Clear();
            return cleared;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<(Type type, TValue value)> IEnumerable<(Type type, TValue value)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        ref TValue Get(int[] indices, out bool success)
        {
            foreach (var index in indices)
            {
                ref var value = ref Get(index, out success);
                if (success) return ref value;
            }

            success = false;
            return ref Dummy<TValue>.Value;
        }

        bool TryGet(int[] indices, out TValue value)
        {
            value = Get(indices, out var success);
            return success;
        }

        bool Has(int[] indices)
        {
            foreach (var index in indices) if (Has(index)) return true;
            return false;
        }
    }
}
