using System;

namespace Entia.Core
{
    public static class Null
    {
        public static T? Some<T>(T value) where T : struct => value;
        public static T? None<T>() where T : struct => null;

        public static bool TryValue<T>(in this T? source, out T value) where T : struct
        {
            value = source ?? default;
            return source.HasValue;
        }

        public static TOut Match<TIn, TOut>(in this TIn? source, Func<TIn, TOut> some, Func<TOut> none) where TIn : struct =>
            source.HasValue ? some(source.Value) : none();

        public static TOut Match<TIn, TOut>(in this TIn? source, Func<TIn, TOut> some, TOut none) where TIn : struct =>
            source.HasValue ? some(source.Value) : none;
    }
}
