using System;
using System.Collections.Generic;

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

        public static ref T Add<T>(ref T[] array, T item)
        {
            var index = array.Length;
            Array.Resize(ref array, index + 1);
            ref var slot = ref array[index];
            slot = item;
            return ref slot;
        }

        public static bool TryAdd<T>(ref Array array, in T item, int index)
        {
            if (array is T[] casted)
            {
                Ensure(ref casted, index + 1);
                casted[index] = item;
                array = casted;
                return true;
            }

            return false;
        }

        public static int GetHashCode<T>(T[] array)
        {
            if (array == null) return 0;
            var hash = 0;
            foreach (var item in array) hash ^= EqualityComparer<T>.Default.GetHashCode(item);
            return hash;
        }
    }
}
