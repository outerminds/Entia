﻿using System;
using System.Collections.Generic;
using Entia.Core.Documentation;

namespace Entia.Core
{
    public static class ArrayUtility
    {
        public static bool Ensure<T>(ref T[] array, int size)
        {
            if (size <= array.Length) return false;
            Array.Resize(ref array, MathUtility.NextPowerOfTwo(size));
            return true;
        }

        public static bool Ensure<T>(ref T[] array, int size, T initial)
        {
            if (size <= array.Length) return false;

            var old = array.Length;
            var @new = MathUtility.NextPowerOfTwo(size);
            Array.Resize(ref array, @new);
            array.Fill(initial, old, @new - old);
            return true;
        }

        public static bool Ensure<T>(ref T[] array, int size, Func<T> initial)
        {
            if (size <= array.Length) return false;

            var old = array.Length;
            var @new = MathUtility.NextPowerOfTwo(size);
            Array.Resize(ref array, @new);
            array.Fill(initial, old, @new - old);
            return true;
        }

        public static bool Ensure<T>(ref T[] array, uint size) => Ensure(ref array, MathUtility.ClampToInt(size));

        public static bool Ensure(ref Array array, Type element, int size)
        {
            if (size <= array.Length) return false;
            Resize(ref array, element, MathUtility.NextPowerOfTwo(size));
            return true;
        }

        public static bool Resize(ref Array array, Type element, int size)
        {
            var target = Array.CreateInstance(element, size);
            array.CopyTo(target, 0);
            array = target;
            return true;
        }

        public static bool Set<T>(ref T[] array, in T item, int index)
        {
            var resized = Ensure(ref array, index + 1);
            array[index] = item;
            return resized;
        }

        public static ref T Add<T>(ref T[] array, in T item)
        {
            var index = array.Length;
            var local = array;
            Array.Resize(ref local, index + 1);
            ref var slot = ref local[index];
            slot = item;
            // NOTE: set the array after the item is set such that no threads can observe the slot uninitialized
            array = local;
            return ref slot;
        }

        public static bool Add<T>(ref T[] array, params T[] items)
        {
            if (items.Length == 0) return false;

            var index = array.Length;
            Array.Resize(ref array, index + items.Length);
            Array.Copy(items, 0, array, index, items.Length);
            return true;
        }

        public static bool Remove<T>(ref T[] array, T item)
        {
            var index = Array.IndexOf(array, item);
            if (index < 0) return false;

            if (array.Length == 1) array = Dummy<T>.Array.Zero;
            else
            {
                var shrunk = new T[array.Length - 1];
                Array.Copy(array, 0, shrunk, 0, index);
                Array.Copy(array, index + 1, shrunk, index, shrunk.Length - index);
                array = shrunk;
            }
            return true;
        }

        public static bool EnsureSet<T>(ref T[] array, in T item, int index)
        {
            var resized = Ensure(ref array, index + 1);
            array[index] = item;
            return resized;
        }

        [ThreadSafe]
        public static int GetHashCode<T>(T[] array)
        {
            if (array == null) return 0;
            var hash = 0;
            var comparer = EqualityComparer<T>.Default;
            foreach (var item in array) hash ^= comparer.GetHashCode(item);
            return hash;
        }

        [ThreadSafe]
        public static int GetHashCode<T>((T[] items, int count) array)
        {
            var hash = array.count;
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < array.count; i++) hash ^= comparer.GetHashCode(array.items[i]);
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

        public static T[] Concatenate<T>(params T[][] arrays)
        {
            if (arrays.Length == 0) return Array.Empty<T>();
            if (arrays.Length == 1) return arrays[0];
            if (arrays.Length == 2) return Concatenate(arrays[0], arrays[1]);

            var count = 0;
            for (int i = 0; i < arrays.Length; i++) count += arrays[i].Length;
            if (count == 0) return Array.Empty<T>();

            var results = new T[count];
            var index = 0;
            for (int i = 0; i < arrays.Length; i++)
            {
                var array = arrays[i];
                Array.Copy(array, 0, results, index, array.Length);
                index += array.Length;
            }
            return results;
        }
    }
}
