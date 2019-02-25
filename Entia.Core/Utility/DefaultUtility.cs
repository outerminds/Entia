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
        static class Cache<T>
        {
            public static readonly Func<T> Provide = GetProvider<T>();
        }

        static readonly Concurrent<TypeMap<object, (Delegate generic, Delegate reflection)>> _providers = new TypeMap<object, (Delegate, Delegate)>();

        public static T Default<T>() => Cache<T>.Provide();
        public static object Default(Type type) => GetProvider(type)();

        static Func<T> GetProvider<T>()
        {
            using (var read = _providers.Read(true))
            {
                if (read.Value.TryGet<T>(out var provider) && provider.generic is Func<T> casted1) return casted1;
                var (generic, reflection) = CreateProviders<T>();
                using (var write = _providers.Write())
                {
                    if (write.Value.TryGet<T>(out provider) && provider.generic is Func<T> casted2) return casted2;
                    write.Value.Set<T>((generic, reflection));
                    return generic;
                }
            }
        }

        static Func<object> GetProvider(Type type)
        {
            using (var read = _providers.Read(true))
            {
                if (read.Value.TryGet(type, out var provider) && provider.reflection is Func<object> casted1) return casted1;
                var reflection = CreateProvider(type);
                using (var write = _providers.Write())
                {
                    if (write.Value.TryGet(type, out provider) && provider.reflection is Func<object> casted2) return casted2;
                    write.Value.Set(type, (default, reflection));
                    return reflection;
                }
            }
        }

        static (Func<T>, Func<object>) CreateProviders<T>()
        {
            var provider = CreateProvider<T>();
            return (provider, () => provider());
        }

        static Func<T> CreateProvider<T>()
        {
            var data = TypeUtility.Cache<T>.Data;
            foreach (var member in data.StaticMembers)
            {
                try
                {
                    if (member.GetCustomAttributes(typeof(DefaultAttribute), true).Any())
                    {
                        switch (member)
                        {
                            case FieldInfo field when field.FieldType.Is<T>():
                                var value = (T)field.GetValue(null);
                                return () => value;
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

        static Func<object> CreateProvider(Type type)
        {
            var data = TypeUtility.GetData(type);
            foreach (var member in data.StaticMembers)
            {
                try
                {
                    if (member.GetCustomAttributes(typeof(DefaultAttribute), true).Any())
                    {
                        switch (member)
                        {
                            case FieldInfo field when field.FieldType.Is(type):
                                var value = field.GetValue(null);
                                return () => value;
                            case PropertyInfo property when property.CanRead && property.PropertyType.Is(type) && property.GetMethod is MethodInfo get:
                                return () => get.Invoke(null, Array.Empty<object>());
                            case MethodInfo method when method.ReturnType.Is(type) && method.GetParameters().None():
                                return () => method.Invoke(null, Array.Empty<object>());
                        }
                    }
                }
                catch { }
            }

            var @default = data.Default;
            return () => @default;
        }
    }
}