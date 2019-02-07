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
                    // NOTE: since the array cannot be pinned because type 'T' cannot be ensured to be blittable, precautions are taken for the unlikely
                    // case where the garbage collector moves the array between 2 pointer get.
                    while (true)
                    {
                        var array = new T[3];
                        var a = Cast<T>.ToPointer(ref array[0]).ToInt64();
                        var b = Cast<T>.ToPointer(ref array[1]).ToInt64();
                        var c = Cast<T>.ToPointer(ref array[2]).ToInt64();
                        if (a > b || a > c || b > c) continue;

                        var sizeAB = b - a;
                        var sizeBC = c - b;
                        var sizeAC = c - a;
                        if (sizeAB == sizeBC && sizeAC == sizeAB + sizeBC)
                        {
                            Value = (int)sizeAB;
                            break;
                        }
                    }
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
