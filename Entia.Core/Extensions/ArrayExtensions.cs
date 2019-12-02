using System;
using System.Collections.Generic;

namespace Entia.Core
{
    public static class ArrayExtensions
    {
        public static bool TryFirst<T>(this T[] array, out T item)
        {
            if (array.Length > 0)
            {
                item = array[0];
                return true;
            }
            item = default;
            return false;
        }

        public static bool TryLast<T>(this T[] array, out T item)
        {
            if (array.Length > 0)
            {
                item = array[array.Length - 1];
                return true;
            }
            item = default;
            return false;
        }

        public static bool TryAt<T>(this T[] array, int index, out T item)
        {
            if (index > 0 && index < array.Length)
            {
                item = array[index];
                return true;
            }
            item = default;
            return false;
        }

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

        public static bool TryGet<T>(in this (T[] items, int count) array, int index, out T item)
        {
            if (index < array.count)
            {
                item = array.items[index];
                return true;
            }

            item = default;
            return false;
        }

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

        public static Slice<T> Slice<T>(this T[] array, int index, int count) => new Slice<T>(array, index, count);
        public static Slice<T> Slice<T>(this T[] array, int count) => array.Slice(0, count);
        public static Slice<T> Slice<T>(this T[] array) => array.Slice(0, array.Length);
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

        public static Array Cast(this Array array, Type type)
        {
            var target = Array.CreateInstance(type, array.Length);
            Array.Copy(array, target, array.Length);
            return target;
        }

        public static T[] Cast<T>(this Array array)
        {
            var target = new T[array.Length];
            Array.Copy(array, target, array.Length);
            return target;
        }

        public static bool Contains<T>(this T[] array, in T item) => Array.IndexOf(array, item, 0, array.Length) >= 0;
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

        public static TResult[] Select<TSource, TResult>(this TSource[] source, Func<TSource, TResult> selector)
        {
            var target = new TResult[source.Length];
            for (int i = 0; i < source.Length; i++) target[i] = selector(source[i]);
            return target;
        }

        public static T[] Prepend<T>(this T[] source, params T[] values)
        {
            var target = new T[source.Length + values.Length];
            Array.Copy(values, 0, target, 0, values.Length);
            Array.Copy(source, 0, target, values.Length, source.Length);
            return target;
        }

        public static T[] Append<T>(this T[] source, params T[] values) => values.Prepend(source);

        public static T[] Take<T>(this T[] source, int count)
        {
            if (count <= 0) return Array.Empty<T>();
            else if (count >= source.Length) return source;
            else
            {
                var target = new T[count];
                Array.Copy(source, 0, target, 0, count);
                return target;
            }
        }

        public static T[] Skip<T>(this T[] source, int count)
        {
            if (count <= 0) return source;
            else if (count >= source.Length) return Array.Empty<T>();
            else
            {
                var target = new T[source.Length - count];
                Array.Copy(source, count, target, 0, target.Length);
                return target;
            }
        }

        public static (TSource1, TSource2)[] Zip<TSource1, TSource2>(this TSource1[] source1, TSource2[] source2)
        {
            var count = Math.Min(source1.Length, source2.Length);
            var target = new (TSource1, TSource2)[count];
            for (int i = 0; i < source1.Length; i++) target[i] = (source1[i], source2[i]);
            return target;
        }

        public static (TSource1, TSource2)[] Zip<TSource1, TSource2>(this (TSource1[], TSource2[]) source) =>
            source.Item1.Zip(source.Item2);

        public static (TSource1[], TSource2[]) Unzip<TSource1, TSource2>(this (TSource1, TSource2)[] source)
        {
            var target = (new TSource1[source.Length], new TSource2[source.Length]);
            for (int i = 0; i < source.Length; i++)
            {
                target.Item1[i] = source[i].Item1;
                target.Item2[i] = source[i].Item2;
            }
            return target;
        }
    }
}
