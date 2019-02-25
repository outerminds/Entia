using System;

namespace Entia.Core
{
    public static class CloneUtility
    {
        static readonly Func<object, object> _clone = typeof(object)
            .GetMethod("MemberwiseClone", TypeUtility.Instance)
            .CreateDelegate<Func<object, object>>();

        public static T Shallow<T>(T value) => typeof(T).IsValueType || Equals(value, null) ? value : (T)_clone(value);
    }
}
