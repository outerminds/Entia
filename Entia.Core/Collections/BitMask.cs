using System;
using System.Collections;
using System.Collections.Generic;
using Entia.Core.Documentation;

namespace Entia.Core
{
    public sealed class BitMask : IEnumerable<int>, IEquatable<BitMask>
    {
        [ThreadSafe]
        public readonly struct Bucket : IComparable<Bucket>
        {
            public const int Size = sizeof(ulong) * 8;

            public readonly uint Index;
            public readonly ulong Mask;

            public Bucket(uint index, ulong mask) { Index = index; Mask = mask; }
            public Bucket(int index, ulong mask) { Index = (uint)index; Mask = mask; }

            int IComparable<Bucket>.CompareTo(Bucket other) => Index.CompareTo(other.Index);

            public static Bucket OfIndex(int index) => new Bucket(index / Size, 1uL << (index % Size));
        }

        public bool IsEmpty => _buckets.count == 0;
        public int Capacity => _buckets.count * Bucket.Size;

        (ulong[] items, int count) _buckets = (new ulong[1], 0);
        int? _hash;

        public BitMask() { }

        public BitMask(params int[] indices)
        {
            foreach (var index in indices) Add(index);
        }

        public BitMask(params BitMask[] masks)
        {
            foreach (var mask in masks) Add(mask);
        }

        [ThreadSafe]
        public bool Has(int index) => Has(Bucket.OfIndex(index));

        [ThreadSafe]
        public bool Has(Bucket bucket) =>
            bucket.Index < _buckets.count &&
            (_buckets.items[bucket.Index] & bucket.Mask) == bucket.Mask;

        [ThreadSafe]
        public bool HasAll(BitMask mask)
        {
            if (mask._buckets.count > _buckets.count) return false;

            var count = Math.Min(_buckets.count, mask._buckets.count);
            for (var i = 0; i < count; i++)
            {
                var bucketA = _buckets.items[i];
                var bucketB = mask._buckets.items[i];
                if ((bucketA & bucketB) != bucketB) return false;
            }

            return true;
        }

        [ThreadSafe]
        public bool HasAny(BitMask mask)
        {
            var count = Math.Min(_buckets.count, mask._buckets.count);
            for (var i = 0; i < count; i++)
            {
                var bucketA = _buckets.items[i];
                var bucketB = mask._buckets.items[i];
                if ((bucketA & bucketB) != 0) return true;
            }

            return false;
        }

        [ThreadSafe]
        public bool HasNone(BitMask mask) => !HasAny(mask);

        public bool Add(int index) => Add(Bucket.OfIndex(index));

        public bool Add(Bucket bucket)
        {
            if (bucket.Mask == 0uL) return false;

            _buckets.Ensure(bucket.Index + 1u);
            ref var current = ref _buckets.items[bucket.Index];
            if (current.Change(current | bucket.Mask))
            {
                RefreshRange();
                return true;
            }

            return false;
        }

        public bool Add(BitMask mask)
        {
            var added = false;
            for (var i = 0; i < mask._buckets.count; i++) added |= Add(new Bucket(i, mask._buckets.items[i]));
            return added;
        }

        public bool Remove(int index) => Remove(Bucket.OfIndex(index));

        public bool Remove(Bucket bucket)
        {
            if (bucket.Mask == 0uL || bucket.Index >= _buckets.count) return false;

            ref var current = ref _buckets.items[bucket.Index];
            if (current.Change(current & ~bucket.Mask))
            {
                RefreshRange();
                return true;
            }

            return false;
        }

        public bool Remove(BitMask mask)
        {
            var removed = false;
            for (var i = 0; i < mask._buckets.count; i++) removed |= Remove(new Bucket(i, mask._buckets.items[i]));
            return removed;
        }

        public bool Clear()
        {
            _hash = null;
            return _buckets.Clear();
        }

        [ThreadSafe]
        public bool Equals(BitMask other)
        {
            if (this == other) return true;
            else if (other == null) return false;
            else if (_buckets.count != other._buckets.count) return false;

            for (var i = 0; i < _buckets.count; i++) if (_buckets.items[i] != other._buckets.items[i]) return false;
            return true;
        }

        [ThreadSafe]
        public override bool Equals(object obj) => obj is BitMask mask && Equals(mask);

        [ThreadSafe]
        public override int GetHashCode()
        {
            if (_hash is int hash) return hash;
            _hash = hash = ArrayUtility.GetHashCode(_buckets);
            return hash;
        }

        void RefreshRange()
        {
            var count = 0;
            for (var i = 0; i < _buckets.items.Length; i++) if (_buckets.items[i] != 0) count = i + 1;
            _buckets.count = count;
            _hash = null;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        [ThreadSafe]
        public IEnumerator<int> GetEnumerator()
        {
            var index = 0;
            for (var i = 0; i < _buckets.count; i++)
            {
                var bucket = _buckets.items[i];
                for (var j = 0; j < Bucket.Size; j++, index++)
                    if ((bucket & (1uL << j)) != 0uL) yield return index;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}