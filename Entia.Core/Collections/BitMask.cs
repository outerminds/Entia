using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Core
{
    public sealed class BitMask : IEnumerable<int>, IEquatable<BitMask>
    {
        public readonly struct Bucket : IComparable<Bucket>
        {
            public const int Size = sizeof(ulong) * 8;

            public readonly int Index;
            public readonly ulong Mask;

            public Bucket(int index, ulong mask) { Index = index; Mask = mask; }

            int IComparable<Bucket>.CompareTo(Bucket other) => Index.CompareTo(other.Index);

            public static Bucket OfIndex(int index) => new Bucket(index / Size, 1uL << (index % Size));
        }

        public static BitMask operator ~(BitMask mask)
        {
            var result = new BitMask(mask);
            for (var i = 0; i < result._buckets.Length; i++) result._buckets[i] = ~result._buckets[i];
            result.RefreshRange();
            return result;
        }

        public static BitMask operator |(BitMask a, BitMask b) => new BitMask(a, b);

        public static BitMask operator &(BitMask a, BitMask b)
        {
            var result = new BitMask();
            var count = Math.Min(a._buckets.Length, b._buckets.Length);
            ArrayUtility.Ensure(ref result._buckets, count);
            for (var i = 0; i < count; i++) result._buckets[i] = a._buckets[i] & b._buckets[i];
            result.RefreshRange();
            return result;
        }

        public bool IsEmpty => _head == 0 && _tail == 0 && _buckets[0] == 0uL;
        public int Count { get; private set; }
        public int Capacity => _buckets.Length * Bucket.Size;

        int _head;
        int _tail;
        int? _hash;
        ulong[] _buckets = new ulong[1];

        public BitMask() { }

        public BitMask(params int[] indices)
        {
            foreach (var index in indices) Add(index);
        }

        public BitMask(params BitMask[] masks)
        {
            foreach (var mask in masks)
                for (var i = 0; i < mask._buckets.Length; i++) Add(new Bucket(i, mask._buckets[i]));
        }

        public bool Has(int index) => Has(Bucket.OfIndex(index));

        public bool Has(Bucket bucket) => _head <= bucket.Index && _tail >= bucket.Index && (_buckets[bucket.Index] & bucket.Mask) == bucket.Mask;

        public bool HasAll(BitMask mask)
        {
            var head = Math.Max(_head, mask._head);
            var tail = Math.Min(_tail, mask._tail);

            for (var i = head; i <= tail; i++)
            {
                var bucketA = _buckets[i];
                var bucketB = mask._buckets[i];
                if ((bucketA & bucketB) != bucketB) return false;
            }

            return true;
        }

        public bool HasAny(BitMask mask)
        {
            var head = Math.Max(_head, mask._head);
            var tail = Math.Min(_tail, mask._tail);

            for (var i = head; i <= tail; i++)
            {
                var bucketA = _buckets[i];
                var bucketB = mask._buckets[i];
                if ((bucketA & bucketB) != 0) return true;
            }

            return false;
        }

        public bool HasNone(BitMask mask) => !HasAny(mask);

        public bool Add(int index) => Add(Bucket.OfIndex(index));

        public bool Add(Bucket bucket)
        {
            if (bucket.Index < 0 || bucket.Mask == 0uL) return false;

            ArrayUtility.Ensure(ref _buckets, bucket.Index + 1);
            ref var current = ref _buckets[bucket.Index];
            var merged = current | bucket.Mask;
            if (current == merged) return false;

            Count++;
            current = merged;
            RefreshRange();
            return true;
        }

        public bool Remove(int index) => Remove(Bucket.OfIndex(index));

        public bool Remove(Bucket bucket)
        {
            if (bucket.Mask == 0uL || bucket.Index < _head || bucket.Index > _tail) return false;

            ref var current = ref _buckets[bucket.Index];
            var filtered = current & ~bucket.Mask;
            if (current == filtered) return false;

            Count--;
            current = filtered;
            RefreshRange();
            return true;
        }

        public void Clear()
        {
            _head = 0;
            _tail = 0;
            _hash = null;
            _buckets.Clear();
        }

        public bool Equals(BitMask other)
        {
            if (_head != other._head || _tail != other._tail) return false;
            for (var i = _head; i <= _tail; i++) if (_buckets[i] != other._buckets[i]) return false;
            return true;
        }

        public override bool Equals(object obj) => obj is BitMask mask && Equals(mask);

        public override int GetHashCode()
        {
            if (_hash is int hash) return hash;

            hash = _head.GetHashCode() ^ _tail.GetHashCode();
            for (var i = _head; i <= _tail; i++) hash ^= _buckets[i].GetHashCode();
            _hash = hash;
            return hash;
        }

        void RefreshRange()
        {
            var head = default(int?);
            var tail = default(int?);

            for (var i = 0; i < _buckets.Length; i++)
            {
                if (_buckets[i] != 0)
                {
                    head = head ?? i;
                    tail = i;
                }
            }

            _head = head ?? 0;
            _tail = tail ?? 0;
            _hash = null;
        }

        public IEnumerator<int> GetEnumerator()
        {
            var index = 0;
            for (var i = _head; i <= _tail; i++)
            {
                var bucket = _buckets[i];
                for (var j = 0; j < Bucket.Size; j++, index++)
                    if ((bucket & (1uL << j)) != 0uL) yield return index;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}