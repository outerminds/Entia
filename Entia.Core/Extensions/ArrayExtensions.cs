﻿using System;
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

        public static Slice<T> Slice<T>(this T[] array, int index, int count) => new Slice<T>(array, index, count);
        public static Slice<T> Slice<T>(this T[] array, int count) => array.Slice(0, count);
        public static Slice<T> Slice<T>(this T[] array) => array.Slice(0, array.Length);

        public static Array Cast(this Array array, Type type)
        {
            var target = Array.CreateInstance(type, array.Length);
            Array.Copy(array, target, array.Length);
            return target;
        }

        public static bool Any<T>(this T[] array) => array.Length > 0;
        public static bool Any(this Array array) => array.Length > 0;
        public static bool None<T>(this T[] array) => !array.Any();
        public static bool None(this Array array) => !array.Any();

        public static T[] Flatten<T>(this T[][] array) => ArrayUtility.Concatenate(array);

        public static T[] Cast<T>(this Array array)
        {
            var target = new T[array.Length];
            Array.Copy(array, target, array.Length);
            return target;
        }

        public static void Iterate<T>(this T[] array, Action<T> action)
        {
            for (int i = 0; i < array.Length; i++) action(array[i]);
        }

        public static void Iterate<T>(this T[] array, InAction<T> action)
        {
            for (int i = 0; i < array.Length; i++) action(array[i]);
        }

        public static void Iterate<T>(this T[] array, RefAction<T> action)
        {
            for (int i = 0; i < array.Length; i++) action(ref array[i]);
        }

        public static bool Contains<T>(this T[] array, in T item) => Array.IndexOf(array, item, 0, array.Length) >= 0;

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

        public static Array RemoveAt(this Array source, Type element, int index)
        {
            var target = Array.CreateInstance(element, source.Length - 1);
            if (index > 0) Array.Copy(source, 0, target, 0, index);
            if (index < source.Length - 1) Array.Copy(source, index + 1, target, index, source.Length - index - 1);
            return target;
        }

        public static Array Prepend(this Array source, Type element, Array values)
        {
            var target = Array.CreateInstance(element, source.Length + values.Length);
            Array.Copy(values, 0, target, 0, values.Length);
            Array.Copy(source, 0, target, values.Length, source.Length);
            return target;
        }

        public static Array Prepend(this Array source, Type element, params object[] values) => Prepend(source, element, (Array)values);
        public static Array Append(this Array source, Type element, params object[] values) => Prepend((Array)values, element, source);
        public static Array Append(this Array source, Type element, Array values) => Prepend(values, element, source);

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

        public static void Shuffle<T>(this T[] array, Random random = null)
        {
            random ??= new Random();
            for (int i = 0; i < array.Length; i++)
            {
                var index = random.Next(array.Length);
                var item = array[i];
                array[i] = array[index];
                array[index] = item;
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
