using System;
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
            public static readonly Func<T> Provide = Provider<T>();
        }

        static readonly Concurrent<TypeMap<object, (Delegate generic, Delegate reflection)>> _providers = new TypeMap<object, (Delegate, Delegate)>();

        public static T Default<T>() => Cache<T>.Provide();
        public static object Default(Type type) => Provider(type)();

        public static Func<T> Provider<T>()
        {
            using (var read = _providers.Read(true))
            {
                if (read.Value.TryGet<T>(out var provider, false, false) && provider.generic is Func<T> casted1) return casted1;
                using (var write = _providers.Write())
                {
                    var (generic, reflection) = CreateProviders<T>();
                    write.Value.Set<T>((generic, reflection));
                    return generic;
                }
            }
        }

        public static Func<object> Provider(Type type)
        {
            using (var read = _providers.Read(true))
            {
                if (read.Value.TryGet(type, out var provider) && provider.reflection is Func<object> casted1) return casted1;
                using (var write = _providers.Write())
                {
                    var reflection = CreateProvider(type);
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
            var data = TypeUtility.GetData<T>();
            foreach (var member in data.StaticMembers.Values)
            {
                try
                {
                    if (member.IsDefined(typeof(DefaultAttribute), true))
                    {
                        switch (member)
                        {
                            case FieldInfo field when field.FieldType.Is<T>():
                                var value = (T)field.GetValue(null);
                                return () => value;
                            case PropertyInfo property when property.PropertyType.Is<T>() && property.GetMethod is MethodInfo getter:
                                return (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), getter);
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
            foreach (var member in data.StaticMembers.Values)
            {
                try
                {
                    if (member.IsDefined(typeof(DefaultAttribute), true))
                    {
                        switch (member)
                        {
                            case FieldInfo field when field.FieldType.Is(type):
                                var value = field.GetValue(null);
                                return () => value;
                            case PropertyInfo property when property.PropertyType.Is(type) && property.GetMethod is MethodInfo getter:
                                return () => getter.Invoke(null, Array.Empty<object>());
                            case MethodInfo method when method.ReturnType.Is(type) && method.GetParameters().None():
                                return () => method.Invoke(null, Array.Empty<object>());
                        }
                    }
                }
                catch { }
            }

            if (data.Default is null) return () => null;
            return () => CloneUtility.Shallow(data.Default);
        }
    }
}