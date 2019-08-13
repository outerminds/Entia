using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Entia.Core;

namespace Entia.Experiment
{
    public interface ISerializer
    {
        bool Serialize(object instance, in SerializeContext context);
        bool Instantiate(out object instance, in DeserializeContext context);
        bool Initialize(ref object instance, in DeserializeContext context);
    }

    public abstract class Serializer<T> : ISerializer
    {
        public abstract bool Serialize(in T instance, in SerializeContext context);
        public bool Deserialize(out T instance, in DeserializeContext context) =>
            Instantiate(out instance, context) && Initialize(ref instance, context);
        public abstract bool Instantiate(out T instance, in DeserializeContext context);
        public abstract bool Initialize(ref T instance, in DeserializeContext context);

        bool ISerializer.Serialize(object instance, in SerializeContext context) => Serialize((T)instance, context);

        bool ISerializer.Instantiate(out object instance, in DeserializeContext context)
        {
            if (Instantiate(out var casted, context))
            {
                instance = casted;
                return true;
            }
            instance = default;
            return false;
        }

        bool ISerializer.Initialize(ref object instance, in DeserializeContext context)
        {
            var casted = (T)instance;
            if (Initialize(ref casted, context))
            {
                instance = casted;
                return true;
            }
            instance = default;
            return false;
        }
    }

    public static class Serializer
    {
        public static class Member
        {
            public static IMember<T> Field<T, TValue>(Field<T, TValue>.Getter get) => new Field<T, TValue>(get);
            public static IMember<T> Property<T, TValue>(Property<T, TValue>.Getter get, Property<T, TValue>.Setter set) => new Property<T, TValue>(get, set);
            public static IMember Reflection(FieldInfo field, ISerializer serializer = null) => new Experiment.Reflection(field);
            public static IMember Reflection(PropertyInfo property, ISerializer serializer = null) => new Experiment.Reflection(property);
        }

        public static class Blittable
        {
            public static Serializer<(T[] items, int count)> Pair<T>() where T : unmanaged => new BlittablePair<T>();

            public static Serializer<T[]> Array<T>() where T : unmanaged => new BlittableArray<T>();
            public static ISerializer Array(Type type, int size)
            {
                if (TryInvoke(nameof(Array), type, out ISerializer value)) return value;
                return new BlittableArray(type, size);
            }

            public static Serializer<T> Object<T>() where T : unmanaged => new BlittableObject<T>();
            public static ISerializer Object(Type type, int size)
            {
                if (TryInvoke(nameof(Object), type, out ISerializer value)) return value;
                return new BlittableObject(type, size);
            }

            static bool TryInvoke<T>(string name, Type type, out T value, params object[] arguments) =>
                Serializer.TryInvoke(typeof(Blittable).StaticMethods(), name, type, out value, arguments);
        }

        public static class Reflection
        {
            public static Serializer<Assembly> Assembly() => new AbstractAssembly();
            public static Serializer<Module> Module() => new AbstractModule();
            public static Serializer<Type> Type() => new AbstractType();
            public static Serializer<MethodInfo> Method() => new AbstractMethod();
            public static Serializer<MemberInfo> Member() => new AbstractMember();
        }

        public static Serializer<string> String() => new ConcreteString();

        public static Serializer<T[]> Array<T>() => new ConcreteArray<T>();
        public static ISerializer Array(Type type)
        {
            if (TryInvoke(nameof(Array), type, out ISerializer value)) return value;
            return new ConcreteArray(type);
        }

        public static Serializer<TFrom> Map<TFrom, TTo>(InFunc<TFrom, TTo> to, InFunc<TTo, TFrom> from) => new Mapper<TFrom, TTo>(to, from);

        public static Serializer<T> Object<T>(Func<T> construct, params IMember<T>[] members) => new ConcreteObject<T>(construct, members);
        public static Serializer<T> Object<T>(params IMember<T>[] members) => new ConcreteObject<T>(members);
        public static ISerializer Object(Type type, params IMember[] members) => new ConcreteObject(type, members);
        public static ISerializer Object(Type type)
        {
            var fields = type.InstanceFields();
            var members = fields.Select(field => Member.Reflection(field)).ToArray();
            return Object(type, members);
        }

        public static Serializer<T> Delegate<T>() where T : Delegate => new ConcreteDelegate<T>();
        public static ISerializer Delegate(Type type)
        {
            if (TryInvoke(nameof(Delegate), type, out ISerializer value)) return value;
            return new ConcreteDelegate(type);
        }

        public static Serializer<List<T>> List<T>() => new ConcreteList<T>();
        public static ISerializer List(Type type)
        {
            if (TryInvoke(nameof(List), type, out ISerializer value)) return value;
            return new ConcreteList(type);
        }

        public static Serializer<Dictionary<TKey, TValue>> Dictionary<TKey, TValue>() => new ConcreteDictionary<TKey, TValue>();
        public static ISerializer Dictionary(Type key, Type value)
        {
            if (TryInvoke(nameof(Dictionary), new[] { key, value }, out ISerializer serializer)) return serializer;
            return new ConcreteDictionary(key, value);
        }

        static bool TryGeneric(ISerializer serializer, out Type type)
        {
            if (serializer.GetType().Bases().TryFirst(@base => @base.Is(typeof(Serializer<>), definition: true), out var generic) &&
                generic.GetGenericArguments().TryFirst(out type)) return true;
            type = default;
            return false;
        }

        static bool TryInvoke(string name, ISerializer serializer, out ISerializer value, params object[] arguments)
        {
            if (TryGeneric(serializer, out var type) && TryInvoke(name, type, out value, arguments)) return true;
            value = default;
            return false;
        }

        static bool TryInvoke<T>(string name, Type type, out T value, params object[] arguments) =>
            TryInvoke(name, new[] { type }, out value, arguments);
        static bool TryInvoke<T>(string name, Type[] types, out T value, params object[] arguments) =>
            TryInvoke(typeof(Serializer).StaticMethods(), name, types, out value, arguments);
        static bool TryInvoke<T>(MethodInfo[] methods, string name, Type type, out T value, params object[] arguments) =>
            TryInvoke(methods, name, new[] { type }, out value, arguments);
        static bool TryInvoke<T>(MethodInfo[] methods, string name, Type[] types, out T value, params object[] arguments)
        {
            if (methods.TryFirst(
                current =>
                    current.IsGenericMethod &&
                    current.Name == name &&
                    current.GetGenericArguments().Length == types.Length &&
                    current.GetParameters().Length == arguments.Length,
                out var method))
            {
                try
                {
                    value = (T)method.MakeGenericMethod(types).Invoke(null, arguments);
                    return true;
                }
                catch { }
            }
            value = default;
            return false;
        }
    }
}