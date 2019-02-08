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
            [StructLayout(LayoutKind.Sequential)]
            struct Layout
            {
                public T Value;
                public Unit End;
            }

            public static readonly int Value = IntPtr.Size;

            static Size()
            {
                if (typeof(T).IsValueType)
                {
                    var pair = default(Layout);
                    var head = Cast<T>.ToPointer(ref pair.Value);
                    var tail = Cast<Unit>.ToPointer(ref pair.End);
                    Value = (int)(tail.ToInt64() - head.ToInt64());
                }
            }
        }

        public static class Cast<T>
        {
            // NOTE: these function pointers are replaced by the one in '_return'.
            public static readonly ReferenceReturn<T> ToReference = _ => throw new NotImplementedException();
            public static readonly PointerReturn<T> ToPointer = (ref T _) => throw new NotImplementedException();

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
            // NOTE: this function pointer is replaced by the one in '_return'.
            public static readonly Return<TSource, TTarget> To = (ref TSource _) => throw new NotImplementedException();

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
