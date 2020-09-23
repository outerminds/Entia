using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Entia.Core.Documentation;

namespace Entia.Core
{
    /// <summary>
    /// A key value store where the key is a type.
    /// Allows to retrieve values faster that a regular dictionary when using the generic getters.
    /// When retrieving a value, the sub or super types of the key used can be considered.
    /// </summary>
    public sealed class TypeMap<TBase, TValue> : IEnumerable<TypeMap<TBase, TValue>.Enumerator, (Type type, TValue value)>
    {
        public struct Enumerator : IEnumerator<(Type type, TValue value)>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public (Type type, TValue value) Current => (_entries[_index].Type, _map._values[_index].value);
            object IEnumerator.Current => Current;

            readonly TypeMap<TBase, TValue> _map;
            int _index;

            public Enumerator(TypeMap<TBase, TValue> map)
            {
                _map = map;
                _index = -1;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext()
            {
                while (++_index < _map._values.Length)
                    if (_map._values[_index].allocated) return true;

                return false;
            }
            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset() => _index = -1;
            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose() => this = default;
        }

        public struct KeyEnumerator : IEnumerator<Type>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public Type Current => _entries[_index].Type;
            object IEnumerator.Current => Current;

            readonly TypeMap<TBase, TValue> _map;
            int _index;

            public KeyEnumerator(TypeMap<TBase, TValue> map)
            {
                _map = map;
                _index = -1;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext()
            {
                while (++_index < _map._values.Length)
                    if (_map._values[_index].allocated) return true;

                return false;
            }
            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset() => _index = -1;
            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose() => this = default;
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
            public ref TValue Current => ref _map._values[_index].value;
            TValue IEnumerator<TValue>.Current => Current;
            object IEnumerator.Current => Current;

            readonly TypeMap<TBase, TValue> _map;
            int _index;

            public ValueEnumerator(TypeMap<TBase, TValue> map)
            {
                _map = map;
                _index = -1;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext()
            {
                while (++_index < _map._values.Length)
                    if (_map._values[_index].allocated) return true;

                return false;
            }

            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset() => _index = -1;
            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose() => this = default;
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
            public static readonly Entry Entry = GetEntry(typeof(T));
        }

        sealed class Entry
        {
            public readonly Type Type;
            public readonly int Index;
            public readonly Entry[] Super;
            public Entry[] Sub;

            public Entry(Type type, int index, Entry[] super, Entry[] sub)
            {
                Type = type;
                Index = index;
                Super = super;
                Sub = sub;
            }
        }

        static Entry[] _entries = { };
        static readonly object _lock = new object();
        static readonly ConcurrentDictionary<Type, Entry> _typeToEntry = new ConcurrentDictionary<Type, Entry>();

        [ThreadSafe]
        static bool TryGetEntry(Type type, out Entry entry)
        {
            entry = GetEntry(type);
            return entry != null;
        }

        [ThreadSafe]
        static Entry GetEntry(Type type)
        {
            static Entry CreateEntry(Type type)
            {
                if (type.Is<TBase>())
                {
                    var data = ReflectionUtility.GetData(type);
                    var super = data.Bases
                        .Concat(data.Interfaces)
                        .SelectMany(@base => @base.Definition.Match(
                            definition => new[] { @base.Type, definition.Type },
                            () => new[] { @base.Type }))
                        .Where(@base => @base.Is<TBase>())
                        .Select(GetEntry)
                        .ToArray();
                    var sub = _entries.Where(current => current.Type.Is(type, true, true)).ToArray();
                    var entry = new Entry(type, _entries.Length, super, sub);

                    // This lock prevents the race condition that would occur if 2 threads were creating an entry
                    // at the same time. A thread may update the '_entries' field between the read and the write of
                    // another thread.
                    // Note that the 'ConcurrentDictionary' garantees that this function will be only called once
                    // per key.
                    // 'Interlocked.CompareExchange' could be used here but a lock seemed like the simpler option.
                    lock (_lock)
                    {
                        Interlocked.Exchange(ref _entries, _entries.Append(entry));
                        foreach (var current in super) Interlocked.Exchange(ref current.Sub, current.Sub.Append(entry));
                    }

                    return entry;
                }
                return default;
            }

            return _typeToEntry.GetOrAdd(type, key => CreateEntry(key));
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
                if (Has(index)) return ref _values[index].value;
                throw new IndexOutOfRangeException();
            }
        }
        public TValue this[Type type]
        {
            [ThreadSafe]
            get => TryGet(type, out var value) ? value : throw new IndexOutOfRangeException();
            set => Set(type, value);
        }
        public int Count => _count;

        (TValue value, bool allocated)[] _values;
        int _count;

        public TypeMap(int capacity = 4) { _values = new (TValue, bool)[capacity]; }

        public TypeMap(params (Type type, TValue value)[] pairs) : this()
        {
            foreach (var (type, value) in pairs) Set(type, value);
        }

        [ThreadSafe]
        public int Index<T>() where T : TBase => Cache<T>.Entry.Index;

        [ThreadSafe]
        public bool TryIndex(Type type, out int index)
        {
            if (TryGetEntry(type, out var entry))
            {
                index = entry.Index;
                return true;
            }
            index = default;
            return false;
        }

        [ThreadSafe]
        public IEnumerable<int> Indices<T>(bool super = false, bool sub = false) where T : TBase =>
            Indices(Cache<T>.Entry, super, sub);

        [ThreadSafe]
        public IEnumerable<int> Indices(Type type, bool super = false, bool sub = false)
        {
            if (_typeToEntry.TryGetValue(type, out var entry)) return Indices(entry, super, sub);
            return Array.Empty<int>();
        }

        [ThreadSafe]
        public ref TValue Get(Type type, out bool success)
        {
            if (TryGetEntry(type, out var entry)) return ref Get(entry, out success);
            success = false;
            return ref Dummy<TValue>.Value;
        }
        [ThreadSafe]
        public ref TValue Get(Type type, out bool success, bool super, bool sub)
        {
            if (TryGetEntry(type, out var entry)) return ref Get(entry, out success, super, sub);
            success = false;
            return ref Dummy<TValue>.Value;
        }
        [ThreadSafe]
        public ref TValue Get<T>(out bool success) where T : TBase => ref Get(Cache<T>.Entry, out success);
        [ThreadSafe]
        public ref TValue Get<T>(out bool success, bool super, bool sub) where T : TBase => ref Get(Cache<T>.Entry, out success, super, sub);
        [ThreadSafe]
        public ref TValue Get(int index, out bool success) => ref Get(_entries[index], out success);
        [ThreadSafe]
        public ref TValue Get(int index, out bool success, bool super, bool sub) => ref Get(_entries[index], out success, super, sub);

        [ThreadSafe]
        public bool TryGet(Type type, out TValue value)
        {
            if (TryGetEntry(type, out var entry)) return TryGet(entry, out value);
            value = default;
            return false;
        }
        [ThreadSafe]
        public bool TryGet(Type type, out TValue value, bool super, bool sub)
        {
            if (TryGetEntry(type, out var entry)) return TryGet(entry, out value, super, sub);
            value = default;
            return false;
        }
        [ThreadSafe]
        public bool TryGet<T>(out TValue value) where T : TBase => TryGet(Cache<T>.Entry, out value);
        [ThreadSafe]
        public bool TryGet<T>(out TValue value, bool super, bool sub) where T : TBase => TryGet(Cache<T>.Entry, out value, super, sub);
        [ThreadSafe]
        public bool TryGet(int index, out TValue value) => TryGet(_entries[index], out value);
        [ThreadSafe]
        public bool TryGet(int index, out TValue value, bool super, bool sub) => TryGet(_entries[index], out value, super, sub);

        [ThreadSafe]
        public bool Has(Type type) => TryGetEntry(type, out var entry) && Has(entry);
        [ThreadSafe]
        public bool Has(Type type, bool super, bool sub) => TryGetEntry(type, out var entry) && Has(entry, super, sub);
        [ThreadSafe]
        public bool Has<T>() where T : TBase => Has(Cache<T>.Entry);
        [ThreadSafe]
        public bool Has<T>(bool super, bool sub) where T : TBase => Has(Cache<T>.Entry, super, sub);
        [ThreadSafe]
        public bool Has(int index) => Has(_entries[index]);
        [ThreadSafe]
        public bool Has(int index, bool super, bool sub) => Has(_entries[index], super, sub);

        public bool Set<T>(in TValue value) where T : TBase => Set(Cache<T>.Entry, value);
        public bool Set(Type type, in TValue value) => TryGetEntry(type, out var entry) && Set(entry, value);
        public bool Set(int index, in TValue value) => Set(_entries[index], value);

        public bool Remove<T>() where T : TBase => Remove(Cache<T>.Entry);
        public bool Remove<T>(bool super, bool sub) where T : TBase => Remove(Cache<T>.Entry, super, sub);
        public bool Remove(Type type) => TryGetEntry(type, out var entry) && Remove(entry);
        public bool Remove(Type type, bool super, bool sub) => TryGetEntry(type, out var entry) && Remove(entry, super, sub);
        public bool Remove(int index) => Remove(_entries[index]);
        public bool Remove(int index, bool super, bool sub) => Remove(_entries[index], super, sub);

        public bool Clear()
        {
            if (_count > 0)
            {
                _values.Clear();
                return true;
            }
            return false;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        [ThreadSafe]
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<(Type type, TValue value)> IEnumerable<(Type type, TValue value)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool TryGet(Entry entry, out TValue value, bool super, bool sub) =>
            TryGet(entry, out value) ||
            (super && TryGet(entry.Super, out value)) ||
            (sub && TryGet(entry.Sub, out value));

        bool TryGet(Entry entry, out TValue value)
        {
            if (entry.Index < _values.Length)
            {
                ref var pair = ref _values[entry.Index];
                value = pair.value;
                return pair.allocated;
            }
            value = default;
            return false;
        }

        bool TryGet(Entry[] entries, out TValue value)
        {
            for (int i = 0; i < entries.Length; i++) if (TryGet(entries[i], out value)) return true;
            value = default;
            return false;
        }

        ref TValue Get(Entry entry, out bool success, bool super, bool sub)
        {
            ref var value = ref Get(entry, out success);
            if (success) return ref value;
            if (super)
            {
                value = ref Get(entry.Super, out success);
                if (success) return ref value;
            }
            if (sub)
            {
                value = ref Get(entry.Sub, out success);
                if (success) return ref value;
            }

            success = false;
            return ref Dummy<TValue>.Value;
        }

        ref TValue Get(Entry entry, out bool success)
        {
            if (entry.Index < _values.Length)
            {
                ref var pair = ref _values[entry.Index];
                success = pair.allocated;
                return ref pair.value;
            }
            success = false;
            return ref Dummy<TValue>.Value;
        }

        ref TValue Get(Entry[] entries, out bool success)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                ref var value = ref Get(entries[i], out success);
                if (success) return ref value;
            }

            success = false;
            return ref Dummy<TValue>.Value;
        }

        bool Has(Entry entry, bool super, bool sub) =>
            Has(entry) || (super && Has(entry.Super) || (sub && Has(entry.Sub)));

        bool Has(Entry entry) => entry.Index < _values.Length && _values[entry.Index].allocated;

        bool Has(Entry[] entries)
        {
            for (int i = 0; i < entries.Length; i++) if (Has(entries[i])) return true;
            return false;
        }

        bool Set(Entry entry, in TValue value)
        {
            ArrayUtility.Ensure(ref _values, entry.Index + 1);
            ref var pair = ref _values[entry.Index];
            pair.value = value;
            if (pair.allocated.Change(true))
            {
                _count++;
                return true;
            }
            return false;
        }

        bool Remove(Entry entry, bool super, bool sub) =>
            Remove(entry) | (super && Remove(entry.Super)) | (sub && Remove(entry.Sub));

        bool Remove(Entry entry)
        {
            if (Has(entry))
            {
                _values[entry.Index] = default;
                _count--;
                return true;
            }
            return false;
        }

        bool Remove(Entry[] entries)
        {
            var removed = false;
            for (int i = 0; i < entries.Length; i++) removed |= Remove(entries[i]);
            return removed;
        }

        IEnumerable<int> Indices(Entry entry, bool super, bool sub)
        {
            yield return entry.Index;
            if (super) for (int i = 0; i < entry.Super.Length; i++) yield return entry.Super[i].Index;
            if (sub) for (int i = 0; i < entry.Sub.Length; i++) yield return entry.Sub[i].Index;
        }
    }
}
