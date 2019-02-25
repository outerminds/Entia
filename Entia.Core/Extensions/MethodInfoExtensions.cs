using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Entia.Core
{
    public static class MethodInfoExtensions
    {
        public static T CreateDelegate<T>(this MethodInfo method) where T : Delegate => Delegate.CreateDelegate(typeof(T), method) as T;
        public static T CreateDelegate<T>(this MethodInfo method, object instance) where T : Delegate => Delegate.CreateDelegate(typeof(T), instance, method) as T;
    }
}
