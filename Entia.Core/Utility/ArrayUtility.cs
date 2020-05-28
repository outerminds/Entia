using System;
using System.Collections.Generic;
using Entia.Core.Documentation;

namespace Entia.Core
{
    public static class ArrayUtility
    {
        public static bool Ensure<T>(ref T[] source, int size)
        {
            if (size <= source.Length) return false;
            Array.Resize(ref source, MathUtility.NextPowerOfTwo(size));
            return true;
        }

        public static bool Ensure<T>(ref T[] source, int size, T initial)
        {
            if (size <= source.Length) return false;

            var old = source.Length;
            var @new = MathUtility.NextPowerOfTwo(size);
            Array.Resize(ref source, @new);
            source.Fill(initial, old, @new - old);
            return true;
        }

        public static bool Ensure<T>(ref T[] source, int size, Func<T> initial)
        {
            if (size <= source.Length) return false;

            var old = source.Length;
            var @new = MathUtility.NextPowerOfTwo(size);
            Array.Resize(ref source, @new);
            source.Fill(initial, old, @new - old);
            return true;
        }

        public static bool Ensure<T>(ref T[] source, uint size) => Ensure(ref source, MathUtility.ClampToInt(size));

        public static bool Ensure(ref Array source, Type element, int size)
        {
            if (size <= source.Length) return false;
            Resize(ref source, element, MathUtility.NextPowerOfTwo(size));
            return true;
        }

        public static bool Resize(ref Array source, Type element, int size)
        {
            var target = Array.CreateInstance(element, size);
            source.CopyTo(target, 0);
            source = target;
            return true;
        }

        public static bool Set<T>(ref T[] source, in T item, int index)
        {
            var resized = Ensure(ref source, index + 1);
            source[index] = item;
            return resized;
        }

        public static bool Prepend<T>(ref T[] source, in T item) => Insert(ref source, 0, item);
        public static bool Prepend<T>(ref T[] source, params T[] items) => Insert(ref source, 0, items);
        public static bool Append<T>(ref T[] source, params T[] items) => Insert(ref source, source.Length, items);
        public static bool Append<T>(ref T[] source, in T item) => Insert(ref source, source.Length, item);

        public static bool Overwrite<T>(ref T[] source, int index, params T[] items)
        {
            if (index < 0 || index > source.Length) return false;
            else if (items.Length == 0) return true;
            else if (index == 0 && items.Length >= source.Length)
            {
                source = items;
                return true;
            }
            else
            {
                var end = index + items.Length;
                var target = new T[Math.Max(source.Length, end)];
                if (index > 0) Array.Copy(source, 0, target, 0, index);
                Array.Copy(items, 0, target, index, items.Length);
                if (end < source.Length) Array.Copy(source, end, target, end, source.Length - end);
                source = target;
                return true;
            }
        }

        public static bool Insert<T>(ref T[] source, int index, params T[] items)
        {
            if (index < 0 || index > source.Length) return false;
            else if (items.Length == 0) return true;
            else if (source.Length == 0)
            {
                source = items;
                return true;
            }
            else
            {
                var target = new T[source.Length + items.Length];
                if (index > 0) Array.Copy(source, 0, target, 0, index);
                Array.Copy(items, 0, target, index, items.Length);
                if (index < source.Length) Array.Copy(source, index, target, items.Length + index, source.Length - index);
                source = target;
                return true;
            }
        }

        public static bool Insert<T>(ref T[] source, int index, in T item)
        {
            if (index < 0 || index > source.Length) return false;
            else
            {
                var target = new T[source.Length + 1];
                if (index > 0) Array.Copy(source, 0, target, 0, index);
                target[index] = item;
                if (index < source.Length) Array.Copy(source, index, target, 1 + index, source.Length - index);
                source = target;
                return true;
            }
        }

        public static bool Remove<T>(ref T[] source, in T item) => RemoveAt(ref source, Array.IndexOf(source, item));

        public static bool RemoveAt<T>(ref T[] source, int index) => RemoveAt(ref source, index, 1);

        public static bool RemoveAt<T>(ref T[] source, int index, int count)
        {
            var end = index + count;
            if (index < 0 || end > source.Length) return false;
            else if (count == 0) return true;
            else if (count == source.Length)
            {
                source = Array.Empty<T>();
                return true;
            }
            else
            {
                var target = new T[source.Length - count];
                if (index > 0) Array.Copy(source, 0, target, 0, index);
                if (end < source.Length) Array.Copy(source, end, target, index, target.Length - index);
                source = target;
                return true;
            }
        }

        public static bool EnsureSet<T>(ref T[] source, in T item, int index)
        {
            var resized = Ensure(ref source, index + 1);
            source[index] = item;
            return resized;
        }

        [ThreadSafe]
        public static int GetHashCode<T>(T[] source)
        {
            if (source == null) return 0;
            var hash = 0;
            var comparer = EqualityComparer<T>.Default;
            foreach (var item in source) hash ^= comparer.GetHashCode(item);
            return hash;
        }

        [ThreadSafe]
        public static int GetHashCode<T>((T[] items, int count) source)
        {
            var hash = source.count;
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < source.count; i++) hash ^= comparer.GetHashCode(source.items[i]);
            return hash;
        }

        public static T[] Concatenate<T>(T[] left, T[] right)
        {
            if (left.Length == 0) return right;
            if (right.Length == 0) return left;

            var count = left.Length + right.Length;
            if (count == 0) return Array.Empty<T>();
            var results = new T[count];
            Array.Copy(left, 0, results, 0, left.Length);
            Array.Copy(right, 0, results, left.Length, right.Length);
            return results;
        }

        public static T[] Concatenate<T>(params T[][] sources)
        {
            if (sources.Length == 0) return Array.Empty<T>();
            if (sources.Length == 1) return sources[0];
            if (sources.Length == 2) return Concatenate(sources[0], sources[1]);

            var count = 0;
            for (int i = 0; i < sources.Length; i++) count += sources[i].Length;
            if (count == 0) return Array.Empty<T>();

            var results = new T[count];
            var index = 0;
            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                Array.Copy(source, 0, results, index, source.Length);
                index += source.Length;
            }
            return results;
        }
    }
}
