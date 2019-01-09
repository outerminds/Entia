using System;
using System.Reflection;

namespace Entia.Core
{
    public static class CloneUtility
    {
        static readonly Func<object, object> _clone = (Func<object, object>)Delegate.CreateDelegate(
            typeof(Func<object, object>),
            typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));

        public static T Shallow<T>(T value) => typeof(T).IsValueType || Equals(value, null) ? value : (T)_clone(value);
    }
}
