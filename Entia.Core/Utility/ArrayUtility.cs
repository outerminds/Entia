using System;
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
            Array.Resize(ref array, index + 1);
            ref var slot = ref array[index];
            slot = item;
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

        public static bool EnsureSet<T>(ref Array array, in T item, int index)
        {
            if (array is T[] casted)
            {
                var resized = Ensure(ref casted, index + 1);
                casted[index] = item;
                array = casted;
                return resized;
            }

            return false;
        }

        [ThreadSafe]
        public static int GetHashCode<T>(T[] array)
        {
            if (array == null) return 0;
            var hash = 0;
            foreach (var item in array) hash ^= EqualityComparer<T>.Default.GetHashCode(item);
            return hash;
        }

        [ThreadSafe]
        public static int GetHashCode<T>((T[] items, int count) array)
        {
            var hash = array.count;
            for (int i = 0; i < array.count; i++) hash ^= EqualityComparer<T>.Default.GetHashCode(array.items[i]);
            return hash;
        }
    }
}
