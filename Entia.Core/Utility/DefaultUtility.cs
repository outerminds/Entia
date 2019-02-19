using System;
using System.Linq;
using System.Reflection;
using Entia.Core.Documentation;

namespace Entia.Core
{
    [ThreadSafe]
    public static class DefaultUtility
    {
        [ThreadSafe]
        public static class Cache<T>
        {
            public static readonly Func<T> Provide = GetProvider<T>();
        }

        static readonly Concurrent<TypeMap<object, Delegate>> _providers = new TypeMap<object, Delegate>();
        static readonly MethodInfo _getDefault = typeof(DefaultUtility).GetMethods(TypeUtility.Static)
            .Where(method => method.Name == nameof(GetProvider) && method.IsGenericMethod)
            .First();

        public static object Default(Type type) => TryGetProvider(type, out var provider) ? provider.DynamicInvoke() : TypeUtility.GetDefault(type);

        static Func<T> GetProvider<T>()
        {
            using (var read = _providers.Read(true))
            {
                if (read.Value.TryGet<T>(out var provider) && provider is Func<T> casted1) return casted1;
                casted1 = CreateProvider<T>();
                using (var write = _providers.Write())
                {
                    if (write.Value.TryGet<T>(out provider) && provider is Func<T> casted2) return casted2;
                    write.Value.Set<T>(casted1);
                    return casted1;
                }
            }
        }

        static bool TryGetProvider(Type type, out Delegate provider)
        {
            provider = default;
            try { provider = _getDefault.MakeGenericMethod(type).Invoke(null, Array.Empty<object>()) as Delegate; }
            catch { }
            return provider != null;
        }

        static Func<T> CreateProvider<T>()
        {
            foreach (var member in typeof(T).GetMembers(TypeUtility.Static))
            {
                try
                {
                    if (member.GetCustomAttributes(true).OfType<DefaultAttribute>().Any())
                    {
                        switch (member)
                        {
                            case FieldInfo field when field.FieldType.Is<T>() && field.GetValue(null) is T value: return () => value;
                            case PropertyInfo property when property.CanRead && property.PropertyType.Is<T>():
                                return (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), property.GetGetMethod());
                            case MethodInfo method when method.ReturnType.Is<T>() && method.GetParameters().None():
                                return (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), method);
                        }
                    }
                }
                catch { }
            }

            return () => default;
        }
    }
}