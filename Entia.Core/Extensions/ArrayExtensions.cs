using System;
using System.Collections.Generic;

namespace Entia.Core
{
    public static class ArrayExtensions
    {
        public static (T[] left, T[] right) Split<T>(this T[] source, int index)
        {
            var left = new T[index];
            var right = new T[source.Length - index];
            Array.Copy(source, 0, left, 0, left.Length);
            Array.Copy(source, index, right, 0, right.Length);
            return (left, right);
        }

        public static void Fill<T>(this T[] source, T value) => source.Fill(value, 0, source.Length);

        public static void Fill<T>(this T[] source, Func<T> provider) => source.Fill(provider, 0, source.Length);

        public static void Fill<T>(this T[] source, T value, int start, int count)
        {
            for (var i = 0; i < count; i++) source[i + start] = value;
        }

        public static void Fill<T>(this T[] source, Func<T> provider, int start, int count)
        {
            for (var i = 0; i < count; i++) source[i + start] = provider();
        }

        public static void Clear<T>(this T[] array) => Array.Clear(array, 0, array.Length);

        public static bool TryGet<T>(ref this (T[] items, int count) pair, int index, out T item)
        {
            if (index < pair.count)
            {
                item = pair.items[index];
                return true;
            }

            item = default;
            return false;
        }

        public static bool Set<T>(ref this (T[] items, int count) pair, int index, in T item)
        {
            pair.count = Math.Max(pair.count, index + 1);
            var resized = pair.Ensure();
            pair.items[index] = item;
            return resized;
        }

        public static T[] ToArray<T>(in this (T[] items, int count) pair)
        {
            if (pair.count == 0) return Array.Empty<T>();
            var array = new T[pair.count];
            Array.Copy(pair.items, 0, array, 0, pair.count);
            return array;
        }

        public static ArrayEnumerable<T> Enumerate<T>(in this (T[] items, int count) pair) => new ArrayEnumerable<T>(pair.items, pair.count);

        public static bool Ensure<T>(ref this (T[] items, int count) pair) => ArrayUtility.Ensure(ref pair.items, pair.count);
        public static bool Ensure<T>(ref this (T[] items, int count) pair, uint size) => ArrayUtility.Ensure(ref pair.items, size);
        public static bool Ensure<T>(ref this (T[] items, int count) pair, int size) => ArrayUtility.Ensure(ref pair.items, size);

        public static void Sort<T>(ref this (T[] items, int count) pair, IComparer<T> comparer) =>
            Array.Sort(pair.items, 0, pair.count, comparer);

        public static ref T Push<T>(ref this (T[] items, int count) pair, T item)
        {
            var index = pair.count++;
            pair.Ensure();
            ref var slot = ref pair.items[index];
            slot = item;
            return ref slot;
        }

        public static ref T Pop<T>(ref this (T[] items, int count) pair) => ref pair.items[--pair.count];

        public static bool TryPop<T>(ref this (T[] items, int count) pair, out T item)
        {
            if (pair.count > 0)
            {
                item = pair.Pop();
                return true;
            }

            item = default;
            return false;
        }

        public static ref T Peek<T>(ref this (T[] items, int count) pair) => ref pair.items[pair.count - 1];
        public static bool TryPeek<T>(ref this (T[] items, int count) pair, out T item)
        {
            if (pair.count > 0)
            {
                item = pair.Peek();
                return true;
            }

            item = default;
            return false;
        }

        public static void Clear<T>(ref this (T[] items, int count) pair)
        {
            Array.Clear(pair.items, 0, pair.count);
            pair.count = 0;
        }
    }
}
