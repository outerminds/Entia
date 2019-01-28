using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Entia.Core
{
    public unsafe static class UnsafeUtility
    {
        struct Container<T> { public T Value; }

        public delegate ref T Return<T>(IntPtr input);
        delegate IntPtr Return(IntPtr input);

        public static class Cache<T>
        {
            public static readonly int Size = IntPtr.Size;
            public static readonly Return<T> Return = Reinterpret.Reference<Return<T>>(new Return(_ => _));

            static Cache()
            {
                if (typeof(T).IsValueType)
                {
                    var array = new T[2];
                    var head = ToPointer(ref array[0]);
                    var tail = ToPointer(ref array[1]);
                    var size = tail.ToInt64() - head.ToInt64();
                    Size = (int)size;
                }
            }
        }

        public static class Reinterpret
        {
            public static void Value<TSource, TTarget>(ref TSource source, ref TTarget target) where TSource : struct where TTarget : struct =>
                Value(ToPointer(ref source), ref target);
            public static void Value<T>(IntPtr source, ref T target) where T : struct
            {
                var reference = __makeref(target);
                var pointer = (IntPtr*)&reference;
                pointer[_index] = source;
                target = __refvalue(reference, T);
            }
            public static T Reference<T>(IntPtr reference) where T : class
            {
                var source = new Container<IntPtr> { Value = reference };
                var target = new Container<T>();
                Value(ref source, ref target);
                return target.Value;
            }
            public static T Reference<T>(object reference) where T : class
            {
                var source = new Container<object> { Value = reference };
                var target = new Container<T>();
                Value(ref source, ref target);
                return target.Value;
            }
        }

        static readonly FieldInfo[] _fields = typeof(TypedReference).GetFields(TypeUtility.Instance);
        static readonly int _index = Array.FindIndex(_fields, field => field.Name == "Value");

        public static IntPtr ToPointer<T>(ref T value)
        {
            var reference = __makeref(value);
            var pointer = (IntPtr*)&reference;
            return pointer[_index];
        }

        public static ref T ToReference<T>(IntPtr pointer) => ref Cache<T>.Return(pointer);
    }
}
