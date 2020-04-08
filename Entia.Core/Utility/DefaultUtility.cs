using System;
using System.Collections.Concurrent;
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

        static readonly ConcurrentDictionary<Type, (Delegate generic, Func<object> reflection)> _providers = new ConcurrentDictionary<Type, (Delegate generic, Func<object> reflection)>();

        public static T Default<T>() => Cache<T>.Provide();
        public static object Default(Type type) => Provider(type)();

        public static Func<T> Provider<T>()
        {
            if (_providers.TryGetValue(typeof(T), out var pair) && pair.generic is Func<T> provider) return provider;
            return (Func<T>)_providers.AddOrUpdate(typeof(T), _ => CreateProviders<T>(), (_, __) => CreateProviders<T>()).generic;
        }

        public static Func<object> Provider(Type type) => _providers.GetOrAdd(type, key => (default, CreateProvider(key))).reflection;

        static (Delegate generic, Func<object> reflection) CreateProviders<T>()
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
                            case PropertyInfo property when property.PropertyType == typeof(T) && property.GetMethod is MethodInfo getter:
                                return (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), getter);
                            case MethodInfo method when method.ReturnType == typeof(T) && method.GetParameters().None():
                                return (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), method);
                        }
                    }
                }
                catch { }
            }
            return CreateDefaultProvider<T>(data);
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
            return CreateDefaultProvider<object>(data);
        }

        static Func<T> CreateDefaultProvider<T>(TypeData data)
        {
            if (typeof(T).IsValueType) return () => default;
            else if (data.Default == null) return () => (T)data.DefaultConstructor?.Invoke(Array.Empty<object>());
            else return () => (T)CloneUtility.Shallow(data.Default);
        }
    }
}