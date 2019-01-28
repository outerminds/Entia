using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Entia.Core
{
    public static class UnsafeUtility
    {
        public delegate IntPtr Return(IntPtr pointer);
        public delegate ref TTarget Return<TSource, TTarget>(ref TSource reference);
        public delegate ref T ReferenceReturn<T>(IntPtr pointer);
        public delegate IntPtr PointerReturn<T>(ref T reference);

        public static class Size<T>
        {
            public static readonly int Value = IntPtr.Size;

            static Size()
            {
                if (typeof(T).IsValueType)
                {
                    var array = new T[2];
                    var head = Cast<T>.ToPointer(ref array[0]);
                    var tail = Cast<T>.ToPointer(ref array[1]);
                    var size = tail.ToInt64() - head.ToInt64();
                    Value = (int)size;
                }
            }
        }

        public static class Cast<T>
        {
            public static readonly ReferenceReturn<T> ToReference = _ => throw null;
            public static readonly PointerReturn<T> ToPointer = (ref T _) => throw null;

            static Cast()
            {
                foreach (var field in _fields)
                {
                    var value = field.GetValue(_return);
                    field.SetValue(ToReference, value);
                    field.SetValue(ToPointer, value);
                }
            }
        }

        public static class Cast<TSource, TTarget>
        {
            public static readonly Return<TSource, TTarget> To = (ref TSource _) => throw null;

            static Cast()
            {
                foreach (var field in _fields)
                {
                    var value = field.GetValue(_return);
                    field.SetValue(To, value);
                }
            }
        }

        static readonly Return _return = _ => _;
        static readonly FieldInfo[] _fields = typeof(Delegate).GetFields(TypeUtility.Instance);
    }
}
