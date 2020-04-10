using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Entia.Core
{
    public unsafe static class UnsafeUtility
    {
        public delegate IntPtr PointerReturn(IntPtr pointer);
        public delegate ref TTarget ReferenceReturn<TSource, TTarget>(in TSource reference);
        public delegate ref T ReferenceReturn<T>(IntPtr pointer);
        public delegate IntPtr PointerReturn<T>(in T reference);
        public delegate IntPtr PointerOffset(IntPtr pointer, int offset);
        public delegate ref T ReferenceOffset<T>(in T reference, int offset);

        static class Cache
        {
            public static readonly PointerReturn<Unit> AsPointer = (in Unit _) => throw new NotImplementedException();

            static Cache() { Copy(_return, AsPointer); }
        }

        static class Cache<T>
        {
            [StructLayout(LayoutKind.Sequential)]
            struct Pair
            {
                public T Value;
                public Unit End;
            }

            // NOTE: these function are replaced in the constructor.
            public static readonly ReferenceReturn<T> As = _ => throw new NotImplementedException();
            public static readonly PointerReturn<T> AsPointer = (in T _) => throw new NotImplementedException();
            public static readonly ReferenceOffset<T> Offset = (in T _, int __) => throw new NotImplementedException();
            public static readonly FieldInfo[] Fields = typeof(T).InstanceFields();

            public static readonly int Size = IntPtr.Size;
            public static readonly (FieldInfo field, int offset)[] Layout = { };

            static Cache()
            {
                Copy(_return, As);
                Copy(_return, AsPointer);
                Copy(_offset, Offset);

                if (typeof(T).IsValueType)
                {
                    var pair = default(Pair);
                    var head = AsPointer(pair.Value);
                    var tail = Cache.AsPointer(pair.End);
                    Size = (int)(tail.ToInt64() - head.ToInt64());

                    // NOTE: layout as it is won't work for reference types
                    var bytes = new byte[Size];
                    bytes.Fill(byte.MaxValue);
                    Layout = new (FieldInfo field, int offset)[Fields.Length];
                    for (int i = 0; i < Fields.Length; i++)
                    {
                        var field = Fields[i];
                        object box = UnsafeUtility.As<byte, T>(ref bytes[0]);
                        field.SetValue(box, default);
                        var unbox = (T)box;
                        var offset = IndexOf(UnsafeUtility.AsPointer(ref unbox), Size);
                        Layout[i] = (field, offset);
                    }
                    Array.Sort(Layout, (a, b) => a.offset.CompareTo(b.offset));
                }
            }

            static int IndexOf(IntPtr pointer, int size)
            {
                var bytes = (byte*)pointer;
                for (int i = 0; i < size; i++) if (bytes[i] == 0) return i;
                return -1;
            }
        }

        static class Cache<TSource, TTarget>
        {
            // NOTE: this function is replaced in the constructor.
            public static readonly ReferenceReturn<TSource, TTarget> As = (in TSource _) => throw new NotImplementedException();

            static Cache() { Copy(_return, As); }
        }

        static readonly PointerReturn _return = _ => _;
        static readonly PointerOffset _offset = (pointer, offset) => pointer + offset;
        static readonly FieldInfo[] _fields = typeof(Delegate).InstanceFields();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set<T>(in T reference, in T value) => AsReference(reference) = value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Size<T>() => Cache<T>.Size;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TTarget As<TSource, TTarget>(ref TSource reference) => ref Cache<TSource, TTarget>.As(reference);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T As<T>(IntPtr pointer) => ref Cache<T>.As(pointer);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr AsPointer<T>(ref T reference) => Cache<T>.AsPointer(reference);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsReference<T>(in T reference) => ref Cache<T, T>.As(reference);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Offset<T>(ref T reference, int offset) => ref Cache<T>.Offset(reference, offset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (FieldInfo field, int offset)[] Layout<T>() => Cache<T>.Layout;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToArray<T>(ref T reference) => ToArray<T, byte>(ref reference);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToArray(IntPtr pointer, int size) => ToArray<byte>(pointer, size);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TTarget[] ToArray<TSource, TTarget>(ref TSource reference) => ToArray<TTarget>(AsPointer(ref reference), Size<TSource>());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(IntPtr pointer, int size)
        {
            var (source, target) = (size, Size<T>());
            var count = source % target == 0 ? source / target : source / target + 1;
            var targets = new T[count];
            Copy(pointer, AsPointer(ref targets[0]), size);
            return targets;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(IntPtr source, IntPtr target, int count)
        {
            for (int i = 0; i < count; i++) ((byte*)target)[i] = ((byte*)source)[i];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(Delegate source, Delegate target)
        {
            foreach (var field in _fields) field.SetValue(target, field.GetValue(source));
        }
    }
}
