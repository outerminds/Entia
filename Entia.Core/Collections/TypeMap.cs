using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Entia.Core.Documentation;

namespace Entia.Core
{
    /// <summary>
    /// Efficient specialized map where keys are types. Supports retrieving a value using the super/sub types of the key,
    /// including interfaces and generic definitions.
    /// <para>
    /// This map can outperform a hash map because it takes advantage of a static generic class to directly retrieve the indices
    /// of keys, thus no search is required in the best case. 'The best case' here means that the generic methods are used and that
    /// super/sub types are not considered.
    /// This does mean that all instances of <see cref="TypeMap{TKey, TValue}"/> with the same generic parameters will use the
    /// same indices for their keys, therefore the memory efficiency of the map will decrease proportionally to the diversity
    /// of usage of the keys between instances. This can be mitigated by using different combinations of <typeparamref name="TKey"/>
    /// and <typeparamref name="TValue"/> since the map will allocate new indices for keys for each unique combination.
    /// </para>
    /// <para>
    /// This map still offers slower non-generic methods for contexts where it is not possible to use the generic ones but these are
    /// expected to perform slightly worse than a hash map lookup, though they still support super/sub type queries.
    /// </para>
    /// <typeparamref name="TKey"/> constrains keys to types that are assignable to it and <typeparamref name="TValue"/> constrains values
    /// to ones that are assignable to it. These generic parameters also serve to mitigate the memory efficiency problem.
    /// </summary>
    public sealed class TypeMap<TKey, TValue> : IEnumerable<TypeMap<TKey, TValue>.Enumerator, (Type type, TValue value)>, ICloneable
    {
        public struct Enumerator : IEnumerator<(Type type, TValue value)>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public (Type type, TValue value) Current => (_entries[_index].Type, _map._values[_index].value);
            object IEnumerator.Current => Current;

            readonly TypeMap<TKey, TValue> _map;
            int _index;

            public Enumerator(TypeMap<TKey, TValue> map)
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

            readonly TypeMap<TKey, TValue> _map;
            int _index;

            public KeyEnumerator(TypeMap<TKey, TValue> map)
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
            readonly TypeMap<TKey, TValue> _map;

            public KeyEnumerable(TypeMap<TKey, TValue> map) { _map = map; }
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

            readonly TypeMap<TKey, TValue> _map;
            int _index;

            public ValueEnumerator(TypeMap<TKey, TValue> map)
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
            readonly TypeMap<TKey, TValue> _map;

            public ValueEnumerable(TypeMap<TKey, TValue> map) { _map = map; }
            /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
            public ValueEnumerator GetEnumerator() => new ValueEnumerator(_map);
            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// Main mechanism by which entries are retrieved for a given key. This allows retrieval of key indices without having
        /// to compute or search for them since they are stored in a static location. Of course, this mechanism can only be used
        /// with the generic methods of the map.
        /// </summary>
        [ThreadSafe]
        static class Cache<T> where T : TKey
        {
            public static readonly Entry Entry = GetEntry(typeof(T));
        }

        /// <summary>
        /// Data structure that holds a reserved <see cref="Entry.Index"/> for a given <see cref="Entry.Type"/> and holds
        /// references to super/sub entries. This <see cref="Entry.Index"/> designates the slot at which all values associated
        /// with the <see cref="Entry.Type"/> will be stored in all <see cref="TypeMap{TKey, TValue}"/> instances with the same
        /// <typeparamref name="TKey"/> and <typeparamref name="TValue"/>.
        /// </summary>
        sealed class Entry
        {
            public readonly Type Type;
            public readonly int Index;
            public Entry[] Super;
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
        static Dictionary<Type, Entry> _typeToEntry = new Dictionary<Type, Entry>();

        [ThreadSafe]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Entry GetEntry(Type type)
        {
            if (_typeToEntry.TryGetValue(type, out var entry)) return entry;
            lock (_typeToEntry)
            {
                if (_typeToEntry.TryGetValue(type, out entry)) return entry;
                // 'super', 'sub' and 'entry' must stay within the lock since they access '_entries' which can be
                // modified by other threads and could otherwise lead to race conditions.
                var super = _entries.Where(type, (current, state) => state.Is(current.Type, true, true)).ToArray();
                var sub = _entries.Where(type, (current, state) => current.Type.Is(state, true, true)).ToArray();
                entry = new Entry(type, _entries.Length, super, sub);
                foreach (var current in super) Interlocked.Exchange(ref current.Sub, current.Sub.Append(entry));
                foreach (var current in sub) Interlocked.Exchange(ref current.Super, current.Super.Append(entry));
                // Technically, a thread may observe an entry that does not yet exist in the entry dictionary by
                // trying to guess the index of that entry, but such hackish speculation will not be considered.
                Interlocked.Exchange(ref _entries, _entries.Append(entry));
                // This must happen last to make sure that no thread can observe an entry that is not full initialized.
                Interlocked.Exchange(ref _typeToEntry, new Dictionary<Type, Entry>(_typeToEntry) { { type, entry } });
                return entry;
            }
        }

        [ThreadSafe]
        public KeyEnumerable Keys => new KeyEnumerable(this);
        [ThreadSafe]
        public ValueEnumerable Values => new ValueEnumerable(this);
        [ThreadSafe]
        public ref TValue this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (Has(index)) return ref _values[index].value;
                throw new IndexOutOfRangeException();
            }
        }
        public TValue this[Type type]
        {
            [ThreadSafe]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => TryGet(type, out var value) ? value : throw new IndexOutOfRangeException();
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Set(type, value);
        }
        [ThreadSafe]
        public int Count => _count;

        (TValue value, bool allocated)[] _values;
        int _count;

        public TypeMap(int capacity = 4) { _values = new (TValue, bool)[capacity]; }

        public TypeMap(params (Type type, TValue value)[] pairs) : this()
        {
            foreach (var (type, value) in pairs) Set(type, value);
        }

        [ThreadSafe]
        public int Index<T>() where T : TKey => Cache<T>.Entry.Index;

        [ThreadSafe]
        public bool TryIndex(Type type, out int index)
        {
            if (GetEntry(type) is Entry entry)
            {
                index = entry.Index;
                return true;
            }
            index = default;
            return false;
        }

        [ThreadSafe]
        public IEnumerable<int> Indices<T>(bool super = false, bool sub = false) where T : TKey =>
            Indices(Cache<T>.Entry, super, sub);

        [ThreadSafe]
        public IEnumerable<int> Indices(Type type, bool super = false, bool sub = false)
        {
            if (GetEntry(type) is Entry entry) return Indices(entry, super, sub);
            return Array.Empty<int>();
        }

        [ThreadSafe]
        public ref TValue Get(Type type, out bool success)
        {
            if (_typeToEntry.TryGetValue(type, out var entry)) return ref Get(entry, out success);
            success = false;
            return ref Dummy<TValue>.Value;
        }
        [ThreadSafe]
        public ref TValue Get(Type type, out bool success, bool super, bool sub) => ref Get(GetEntry(type), out success, super, sub);
        [ThreadSafe]
        public ref TValue Get<T>(out bool success) where T : TKey => ref Get(Cache<T>.Entry, out success);
        [ThreadSafe]
        public ref TValue Get<T>(out bool success, bool super, bool sub) where T : TKey => ref Get(Cache<T>.Entry, out success, super, sub);
        [ThreadSafe]
        public ref TValue Get(int index, out bool success) => ref Get(_entries[index], out success);
        [ThreadSafe]
        public ref TValue Get(int index, out bool success, bool super, bool sub) => ref Get(_entries[index], out success, super, sub);

        [ThreadSafe]
        public bool TryGet(Type type, out TValue value)
        {
            if (_typeToEntry.TryGetValue(type, out var entry)) return TryGet(entry, out value);
            value = default;
            return false;
        }
        [ThreadSafe]
        public bool TryGet(Type type, out TValue value, bool super, bool sub) => TryGet(GetEntry(type), out value, super, sub);
        [ThreadSafe]
        public bool TryGet<T>(out TValue value) where T : TKey => TryGet(Cache<T>.Entry, out value);
        [ThreadSafe]
        public bool TryGet<T>(out TValue value, bool super, bool sub) where T : TKey => TryGet(Cache<T>.Entry, out value, super, sub);
        [ThreadSafe]
        public bool TryGet(int index, out TValue value) => TryGet(_entries[index], out value);
        [ThreadSafe]
        public bool TryGet(int index, out TValue value, bool super, bool sub) => TryGet(_entries[index], out value, super, sub);

        [ThreadSafe]
        public bool Has(Type type) => _typeToEntry.TryGetValue(type, out var entry) && Has(entry);
        [ThreadSafe]
        public bool Has(Type type, bool super, bool sub) => Has(GetEntry(type), super, sub);
        [ThreadSafe]
        public bool Has<T>() where T : TKey => Has(Cache<T>.Entry);
        [ThreadSafe]
        public bool Has<T>(bool super, bool sub) where T : TKey => Has(Cache<T>.Entry, super, sub);
        [ThreadSafe]
        public bool Has(int index) => Has(_entries[index]);
        [ThreadSafe]
        public bool Has(int index, bool super, bool sub) => Has(_entries[index], super, sub);

        public bool Set<T>(in TValue value) where T : TKey => Set(Cache<T>.Entry, value);
        public bool Set(Type type, in TValue value) => GetEntry(type) is Entry entry && Set(entry, value);
        public bool Set(int index, in TValue value) => Set(_entries[index], value);

        public bool Remove<T>() where T : TKey => Remove(Cache<T>.Entry);
        public bool Remove<T>(bool super, bool sub) where T : TKey => Remove(Cache<T>.Entry, super, sub);
        public bool Remove(Type type) => _typeToEntry.TryGetValue(type, out var entry) && Remove(entry);
        public bool Remove(Type type, bool super, bool sub) => Remove(GetEntry(type), super, sub);
        public bool Remove(int index) => Remove(_entries[index]);
        public bool Remove(int index, bool super, bool sub) => Remove(_entries[index], super, sub);

        public bool Clear()
        {
            if (_count.Change(0))
            {
                _values.Clear();
                return true;
            }
            return false;
        }

        public TypeMap<TKey, TValue> Clone()
        {
            var clone = CloneUtility.Shallow(this);
            clone._values = CloneUtility.Shallow(clone._values);
            return clone;
        }
        object ICloneable.Clone() => Clone();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        [ThreadSafe]
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<(Type type, TValue value)> IEnumerable<(Type type, TValue value)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool TryGet(Entry entry, out TValue value, bool super, bool sub) =>
            TryGet(entry, out value) ||
            (super && TryGet(entry.Super, out value)) ||
            (sub && TryGet(entry.Sub, out value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool TryGet(Entry entry, out TValue value)
        {
            if (entry.Index < _values.Length)
            {
                var allocated = false;
                (value, allocated) = _values[entry.Index];
                return allocated;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool TryGet(Entry[] entries, out TValue value)
        {
            for (int i = 0; i < entries.Length; i++) if (TryGet(entries[i], out value)) return true;
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Has(Entry entry, bool super, bool sub) =>
            Has(entry) || (super && Has(entry.Super)) || (sub && Has(entry.Sub));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Has(Entry entry) => entry.Index < _values.Length && _values[entry.Index].allocated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Has(Entry[] entries)
        {
            for (int i = 0; i < entries.Length; i++) if (Has(entries[i])) return true;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Remove(Entry entry, bool super, bool sub) =>
            Remove(entry) | (super && Remove(entry.Super)) | (sub && Remove(entry.Sub));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Remove(Entry entry)
        {
            if (entry.Index < _values.Length)
            {
                ref var pair = ref _values[entry.Index];
                if (pair.allocated.Change(false))
                {
                    pair.value = default;
                    _count--;
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
