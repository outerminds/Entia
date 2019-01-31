using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Entia.Core.Documentation;

namespace Entia.Core
{
    public sealed class TypeMap<TBase, TValue> : IEnumerable<(Type type, TValue value)>
    {
        public struct Enumerator : IEnumerator<(Type type, TValue value)>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public (Type type, TValue value) Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => (_state.Read(_index, (in State state, in int index) => state.Types[index]), _map._values.items[_index]);
            }
            object IEnumerator.Current => Current;

            TypeMap<TBase, TValue> _map;
            int _index;

            public Enumerator(TypeMap<TBase, TValue> map)
            {
                _map = map;
                _index = -1;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_index < _map._allocated.Length)
                    if (_map._allocated[_index]) return true;

                return false;
            }
            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset() => _index = -1;
            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose() => _map = null;
        }

        public struct KeyEnumerator : IEnumerator<Type>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public Type Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _state.Read(_index, (in State state, in int index) => state.Types[index]);
            }
            object IEnumerator.Current => Current;

            TypeMap<TBase, TValue> _map;
            int _index;

            public KeyEnumerator(TypeMap<TBase, TValue> map)
            {
                _map = map;
                _index = -1;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_index < _map._allocated.Length)
                    if (_map._allocated[_index]) return true;

                return false;
            }
            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset() => _index = -1;
            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose() => _map = null;
        }

        public readonly struct KeyEnumerable : IEnumerable<Type>
        {
            readonly TypeMap<TBase, TValue> _map;

            public KeyEnumerable(TypeMap<TBase, TValue> map) { _map = map; }
            /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
            public KeyEnumerator GetEnumerator() => new KeyEnumerator(_map);
            IEnumerator<Type> IEnumerable<Type>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct ValueEnumerator : IEnumerator<TValue>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public ref TValue Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _map._values.items[_index];
            }
            TValue IEnumerator<TValue>.Current => Current;
            object IEnumerator.Current => Current;

            TypeMap<TBase, TValue> _map;
            int _index;

            public ValueEnumerator(TypeMap<TBase, TValue> map)
            {
                _map = map;
                _index = -1;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_index < _map._allocated.Length)
                    if (_map._allocated[_index]) return true;

                return false;
            }

            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset() => _index = -1;
            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose() => _map = null;
        }

        public readonly struct ValueEnumerable : IEnumerable<TValue>
        {
            readonly TypeMap<TBase, TValue> _map;

            public ValueEnumerable(TypeMap<TBase, TValue> map) { _map = map; }
            /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
            public ValueEnumerator GetEnumerator() => new ValueEnumerator(_map);
            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [ThreadSafe]
        static class Cache<T> where T : TBase
        {
            public static readonly int Index = GetIndex(typeof(T));
            public static readonly List<int> Indices = GetIndices(typeof(T));
        }

        struct State
        {
            public List<Type> Types;
            public Dictionary<Type, int> TypeToIndex;
            public Dictionary<Type, List<int>> TypeToIndices;
        }

        static readonly Concurrent<State> _state = new State
        {
            Types = new List<Type>(),
            TypeToIndex = new Dictionary<Type, int>(),
            TypeToIndices = new Dictionary<Type, List<int>>()
        };

        [ThreadSafe]
        static int GetIndex(Type type)
        {
            using (var read = _state.Read(true))
            {
                if (read.Value.TypeToIndex.TryGetValue(type, out var index)) return index;
                return ReserveIndex(type);
            }
        }

        [ThreadSafe]
        static int ReserveIndex(Type type)
        {
            if (!type.IsGenericTypeDefinition && !type.IsAbstract && type.Is<TBase>())
            {
                var children = type.Hierarchy()
                    .SelectMany(child => child.IsGenericType ? new[] { child, child.GetGenericTypeDefinition() } : new[] { child })
                    .Where(TypeUtility.Is<TBase>)
                    .ToArray();
                using (var write = _state.Write())
                {
                    if (write.Value.TypeToIndex.TryGetValue(type, out var index)) return index;
                    index = write.Value.Types.Count;
                    write.Value.Types.Add(type);
                    write.Value.TypeToIndex[type] = index;
                    foreach (var child in children) GetIndices(child).Add(index);
                    return index;
                }
            }

            return -1;
        }

        [ThreadSafe]
        static List<int> GetIndices(Type type)
        {
            using (var read = _state.Read(true))
            {
                if (read.Value.TypeToIndices.TryGetValue(type, out var indices)) return indices;
                using (var write = _state.Write())
                {
                    if (write.Value.TypeToIndices.TryGetValue(type, out indices)) return indices;
                    return write.Value.TypeToIndices[type] = new List<int>();
                }
            }
        }

        [ThreadSafe]
        static bool TryGetIndices(Type type, out List<int> indices)
        {
            using (var read = _state.Read()) return read.Value.TypeToIndices.TryGetValue(type, out indices);
        }

        [ThreadSafe]
        public KeyEnumerable Keys => new KeyEnumerable(this);
        [ThreadSafe]
        public ValueEnumerable Values => new ValueEnumerable(this);
        [ThreadSafe]
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
            [ThreadSafe]
            get => TryGet(type, out var value) ? value : throw new IndexOutOfRangeException();
            set => Set(type, value);
        }

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

        [ThreadSafe]
        public int Index<T>() where T : TBase => Cache<T>.Index;
        [ThreadSafe]
        public bool TryIndex(Type concrete, out int index) => (index = GetIndex(concrete)) >= 0;

        [ThreadSafe]
        public ref TValue Get<T>(out bool success, bool inherit = false) where T : TBase
        {
            if (inherit) return ref Get(Cache<T>.Indices, out success);
            else return ref Get(Cache<T>.Index, out success);
        }

        [ThreadSafe]
        public ref TValue Get(Type type, out bool success, bool inherit = false)
        {
            if (inherit)
            {
                if (TryGetIndices(type, out var indices))
                    return ref Get(indices, out success);
            }
            else
            {
                if (TryIndex(type, out var index))
                    return ref Get(index, out success);
            }

            success = false;
            return ref Dummy<TValue>.Value;
        }

        [ThreadSafe]
        public ref TValue Get(int index, out bool success)
        {
            if (success = Has(index)) return ref _values.items[index];
            return ref Dummy<TValue>.Value;
        }

        [ThreadSafe]
        public bool TryGet<T>(out TValue value, bool inherit = false) where T : TBase =>
            inherit ? TryGet(Cache<T>.Indices, out value) : TryGet(Cache<T>.Index, out value);

        [ThreadSafe]
        public bool TryGet(Type type, out TValue value, bool inherit = false)
        {
            if (inherit)
            {
                if (TryGetIndices(type, out var indices))
                    return TryGet(indices, out value);
            }
            else
            {
                if (TryIndex(type, out var index))
                    return TryGet(index, out value);
            }

            value = default;
            return false;
        }

        [ThreadSafe]
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
        public bool Set(Type type, in TValue value) => TryIndex(type, out var index) && Set(index, value);
        public bool Set(Type type, in TValue value, out int index) => TryIndex(type, out index) && Set(index, value);
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

        [ThreadSafe]
        public bool Has(Type type, bool inherit = false) => inherit ?
            TryGetIndices(type, out var indices) && Has(indices) :
            TryIndex(type, out var index) && Has(index);
        [ThreadSafe]
        public bool Has<T>(bool inherit = false) where T : TBase => inherit ? Has(Cache<T>.Indices) : Has(Cache<T>.Index);
        [ThreadSafe]
        public bool Has(int index) => index >= 0 && index < _allocated.Length && _allocated[index];

        public bool Remove<T>() where T : TBase => Remove(Cache<T>.Index);
        public bool Remove(Type type) => TryIndex(type, out var index) && Remove(index);
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
            _allocated.Clear();
            return _values.Clear();
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        [ThreadSafe]
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<(Type type, TValue value)> IEnumerable<(Type type, TValue value)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        ref TValue Get(List<int> indices, out bool success)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                ref var value = ref Get(indices[i], out success);
                if (success) return ref value;
            }

            success = false;
            return ref Dummy<TValue>.Value;
        }

        bool TryGet(List<int> indices, out TValue value)
        {
            value = Get(indices, out var success);
            return success;
        }

        bool Has(List<int> indices)
        {
            for (int i = 0; i < indices.Count; i++) if (Has(indices[i])) return true;
            return false;
        }
    }
}
