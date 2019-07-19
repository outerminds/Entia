using System;

namespace Entia.Core
{
    public static class CloneUtility
    {
        static class Cache<T>
        {
            public static Func<T, T> Clone = typeof(T).IsValueType ?
                new Func<T, T>((value => value)) :
                new Func<T, T>((value => value == null ? default : (T)_clone(value)));
        }

        static readonly Func<object, object> _clone = typeof(object)
            .GetMethod("MemberwiseClone", TypeUtility.Instance)
            .CreateDelegate<Func<object, object>>();

        public static object Shallow(object value) => value is null ? null : _clone(value);
        public static T Shallow<T>(T value) => Cache<T>.Clone(value);
    }
}
