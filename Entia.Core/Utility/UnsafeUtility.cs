using System;
using System.Runtime.CompilerServices;

namespace Entia.Core
{
    public unsafe static class UnsafeUtility
    {
        public static class Cache<T> where T : unmanaged
        {
            public static readonly int Size = sizeof(T);
        }

        public static class Cast<TFrom, TTo>
        {
            public static readonly Func<TFrom, TTo> To;
            public static readonly TryFunc<TFrom, TTo> TryTo;

            static Cast()
            {
                if (typeof(TFrom) == typeof(TTo))
                {
                    Func<TFrom, TFrom> caster = _ => _;
                    To = caster as Func<TFrom, TTo>;

                    TryFunc<TFrom, TFrom> tryCaster = (TFrom from, out TFrom to) => { to = from; return true; };
                    TryTo = tryCaster as TryFunc<TFrom, TTo>;
                }
                else
                {
                    To = _ => default;
                    TryTo = (TFrom _, out TTo to) => { to = default; return false; };
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reinterpret<TSource, TTarget>(ref TSource source, ref TTarget target) => Reinterpret(ToPointer(ref source), ref target);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reinterpret<T>(IntPtr source, ref T target) => Reinterpret<T>(source.ToPointer(), ref target);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reinterpret<T>(void* source, ref T target)
        {
            var reference = __makeref(target);
            *(void**)&reference = source;
            target = __refvalue(reference, T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* ToPointer<T>(ref T value)
        {
            var reference = __makeref(value);
            return *(void**)&reference;
        }
    }
}
