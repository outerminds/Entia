using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Entia.Core.Documentation;

namespace Entia.Core
{
    public sealed class TypeMap<TBase, TValue> : IEnumerable<TypeMap<TBase, TValue>.Enumerator, (Type type, TValue value)>
    {
        public struct Enumerator : IEnumerator<(Type type, TValue value)>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public (Type type, TValue value) Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { using (var read = _state.Read()) return (read.Value.Types[_index], _map._values.items[_index]); }
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

        public readonly struct KeyEnumerable : IEnumerable<KeyEnumerator, Type>
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

        public readonly struct ValueEnumerable : IEnumerable<ValueEnumerator, TValue>
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
            public static readonly List<int> Super = _state.Read(state => state.Super[Index]);
            public static readonly List<int> Sub = _state.Read(state => state.Sub[Index]);
        }

        struct State
        {
            public Dictionary<Type, int> Indices;
            public List<Type> Types;
            public List<List<int>> Super;
            public List<List<int>> Sub;
        }

        static readonly Concurrent<State> _state = new State
        {
            Indices = new Dictionary<Type, int>(),
            Types = new List<Type>(),
            Super = new List<List<int>>(),
            Sub = new List<List<int>>(),
        };

        static bool TryGetIndices(Type type, out (int index, List<int> super, List<int> sub) indices)
        {
            using (var read = _state.Read(true))
            {
                var index = read.Value.Indices.TryGetValue(type, out var value) ? value : ReserveIndex(type);
                if (index >= 0)
                {
                    indices = (index, read.Value.Super[index], read.Value.Sub[index]);
                    return true;
                }

                indices = default;
                return false;
            }
        }

        [ThreadSafe]
        static int GetIndex(Type concrete)
        {
            using (var read = _state.Read(true))
            {
                if (read.Value.Indices.TryGetValue(concrete, out var index)) return index;
                return ReserveIndex(concrete);
            }
        }

        [ThreadSafe]
        static int ReserveIndex(Type type)
        {
            if (type.Is<TBase>())
            {
                using (var write = _state.Write())
                {
                    if (write.Value.Indices.TryGetValue(type, out var index)) return index;

                    var super = type.Bases()
                        .Concat(type.Interfaces())
                        .SelectMany(ancestor => ancestor.IsGenericType ? new[] { ancestor, ancestor.GetGenericTypeDefinition() } : new[] { ancestor })
                        .Where(TypeUtility.Is<TBase>)
                        .Select(current => (type: current, index: GetIndex(current)))
                        .ToArray();
                    var sub = write.Value.Types
                        .Select((current, i) => (type: current, index: i))
                        .Where(pair => pair.type.Is(type, true, true))
                        .ToArray();
                    index = write.Value.Types.Count;
                    write.Value.Types.Add(type);
                    write.Value.Indices[type] = index;
                    write.Value.Super.Add(new List<int>(super.Select(pair => pair.index)));
                    write.Value.Sub.Add(new List<int>(sub.Select(pair => pair.index)));
                    foreach (var pair in super) write.Value.Sub[pair.index].Add(index);
                    return index;
                }
            }

            return -1;
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
            get => TryGet(type, out var value, false, false) ? value : throw new IndexOutOfRangeException();
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
        public ref TValue Get<T>(out bool success, bool super = false, bool sub = false) where T : TBase
        {
            ref var value = ref Get(Cache<T>.Index, out success);
            if (success) return ref value;
            if (super)
            {
                value = ref Get(Cache<T>.Super, out success);
                if (success) return ref value;
            }
            if (sub)
            {
                value = ref Get(Cache<T>.Sub, out success);
                if (success) return ref value;
            }

            return ref Dummy<TValue>.Value;
        }

        [ThreadSafe]
        public ref TValue Get(Type type, out bool success, bool super = false, bool sub = false)
        {
            if (TryGetIndices(type, out var indices))
            {
                ref var value = ref Get(indices.index, out success);
                if (success) return ref value;
                if (super)
                {
                    value = ref Get(indices.super, out success);
                    if (success) return ref value;
                }
                if (sub)
                {
                    value = ref Get(indices.sub, out success);
                    if (success) return ref value;
                }
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
        public bool TryGet<T>(out TValue value, bool super = false, bool sub = false) where T : TBase =>
            TryGet(Cache<T>.Index, out value) || (super && TryGet(Cache<T>.Super, out value)) || (sub && TryGet(Cache<T>.Sub, out value));

        [ThreadSafe]
        public bool TryGet(Type type, out TValue value, bool super = false, bool sub = false)
        {
            if (TryGetIndices(type, out var indices))
                return TryGet(indices.index, out value) ||
                    (super && TryGet(indices.super, out value)) ||
                    (sub && TryGet(indices.sub, out value));

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
        public bool Has(Type type, bool super = false, bool sub = false) => TryGetIndices(type, out var indices) &&
            (Has(indices.index) || (super && Has(indices.super) || (sub && Has(indices.sub))));
        [ThreadSafe]
        public bool Has<T>(bool super = false, bool sub = false) where T : TBase =>
            Has(Cache<T>.Index) || (super && Has(Cache<T>.Super)) || (sub && Has(Cache<T>.Sub));
        [ThreadSafe]
        public bool Has(int index) => index >= 0 && index < _allocated.Length && _allocated[index];

        public bool Remove<T>(bool super = false, bool sub = false) where T : TBase =>
            Remove(Cache<T>.Index) | (super && Remove(Cache<T>.Super)) | (sub && Remove(Cache<T>.Sub));
        public bool Remove(Type type, bool super = false, bool sub = false) => TryGetIndices(type, out var indices) &&
            Remove(indices.index) | (super && Remove(indices.super)) | (sub && Remove(indices.sub));
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

        bool TryGet(List<int> indices, out TValue value)
        {
            value = Get(indices, out var success);
            return success;
        }

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

        bool Has(List<int> indices)
        {
            for (int i = 0; i < indices.Count; i++) if (Has(indices[i])) return true;
            return false;
        }

        bool Remove(List<int> indices)
        {
            var removed = false;
            for (int i = 0; i < indices.Count; i++) removed |= Remove(indices[i]);
            return removed;
        }
    }
}
