using System;
using System.Collections.Generic;

namespace Entia.Core
{
    public static class TupleArrayExtensions
    {
        public static bool Set<T>(ref this (T[] items, int count) array, int index, in T item)
        {
            array.count = Math.Max(array.count, index + 1);
            var resized = array.Ensure();
            array.items[index] = item;
            return resized;
        }

        public static T[] ToArray<T>(in this (T[] items, int count) array)
        {
            if (array.count == 0) return Array.Empty<T>();
            var target = new T[array.count];
            Array.Copy(array.items, 0, target, 0, array.count);
            return target;
        }

        public static T[] ToArray<T>(in this (Array items, int count) array)
        {
            if (array.count == 0) return Array.Empty<T>();
            var target = new T[array.count];
            Array.Copy(array.items, 0, target, 0, array.count);
            return target;
        }

        public static Slice<T> Slice<T>(in this (T[] items, int count) array) => array.items.Slice(0, array.count);
        public static Slice<T> Slice<T>(in this (T[] items, int count) array, int count) => array.Slice(0, count);
        public static Slice<T> Slice<T>(in this (T[] items, int count) array, int index, int count) => array.items.Slice(index, count);

        public static bool Ensure<T>(ref this (T[] items, int count) array) => ArrayUtility.Ensure(ref array.items, array.count);
        public static bool Ensure<T>(ref this (T[] items, int count) array, uint size) => ArrayUtility.Ensure(ref array.items, size);
        public static bool Ensure<T>(ref this (T[] items, int count) array, int size) => ArrayUtility.Ensure(ref array.items, size);

        public static void Sort<T>(ref this (T[] items, int count) array, IComparer<T> comparer) =>
            Array.Sort(array.items, 0, array.count, comparer);

        public static void Push<T>(ref this (T[] items, int count) array, params T[] items) => array.Insert(array.count, items);
        public static ref T Push<T>(ref this (T[] items, int count) array, in T item) => ref array.Insert(array.count, item);
        public static ref T Push<T>(ref this (Array items, int count) array, in T item) => ref array.Insert(array.count, item);

        public static ref T Pop<T>(ref this (T[] items, int count) array) => ref array.items[--array.count];

        public static bool TryPop<T>(ref this (T[] items, int count) array, out T item)
        {
            if (array.count > 0)
            {
                item = array.Pop();
                return true;
            }

            item = default;
            return false;
        }

        public static ref T Peek<T>(ref this (T[] items, int count) array) => ref array.items[array.count - 1];
        public static bool TryPeek<T>(ref this (T[] items, int count) array, out T item)
        {
            if (array.count > 0)
            {
                item = array.Peek();
                return true;
            }

            item = default;
            return false;
        }

        public static bool Contains<T>(in this (T[] items, int count) array, in T item) => array.IndexOf(item) >= 0;
        public static bool Contains<T>(in this (Array items, int count) array, in T item) => array.IndexOf(item) >= 0;

        public static void Insert<T>(ref this (T[] items, int count) array, int index, params T[] items)
        {
            for (int i = 0; i < items.Length; i++) array.Insert(index + i, items[i]);
        }

        public static ref T Insert<T>(ref this (T[] items, int count) array, int index, in T item)
        {
            var last = array.count++;
            array.Ensure();
            if (index < last) Array.Copy(array.items, index, array.items, index + 1, last - index);
            ref var slot = ref array.items[index];
            slot = item;
            return ref slot;
        }

        public static ref T Insert<T>(ref this (Array items, int count) array, int index, in T item)
        {
            var items = array.items as T[];
            var last = array.count++;
            if (ArrayUtility.Ensure(ref items, array.count)) array.items = items;
            if (index < last) Array.Copy(array.items, index, array.items, index + 1, last - index);
            ref var slot = ref items[index];
            slot = item;
            return ref slot;
        }

        public static bool Remove<T>(ref this (T[] items, int count) array, in T item) => array.RemoveAt(array.IndexOf(item));
        public static bool Remove<T>(ref this (Array items, int count) array, in T item) => array.RemoveAt(array.IndexOf(item));

        public static bool RemoveAt<T>(ref this (T[] items, int count) array, int index)
        {
            if (index < 0 || index >= array.count) return false;
            array.count--;
            if (index < array.count) Array.Copy(array.items, index + 1, array.items, index, array.count - index);
            return true;
        }

        public static bool RemoveAt(ref this (Array items, int count) array, int index)
        {
            if (index < 0 || index >= array.count) return false;
            array.count--;
            if (index < array.count) Array.Copy(array.items, index + 1, array.items, index, array.count - index);
            return true;
        }

        public static int IndexOf<T>(in this (T[] items, int count) array, in T item) =>
            Array.IndexOf(array.items, item, 0, array.count);

        public static int IndexOf<T>(in this (Array items, int count) array, in T item) =>
            Array.IndexOf(array.items, item, 0, array.count);

        public static bool Clear<T>(ref this (T[] items, int count) array)
        {
            Array.Clear(array.items, 0, array.count);
            return array.count.Change(0);
        }

        public static bool Clear(ref this (Array items, int count) array)
        {
            Array.Clear(array.items, 0, array.count);
            return array.count.Change(0);
        }

        public static (T[] items, int count) Clone<T>(in this (T[] items, int count) array) =>
            (array.items.Clone() as T[], array.count);
    }
}