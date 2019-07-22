using System;
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
        bool Clone(object instance, out object clone, in CloneContext context);
    }

    public abstract class Serializer<T> : ISerializer
    {
        public abstract bool Serialize(in T instance, in SerializeContext context);
        public abstract bool Instantiate(out T instance, in DeserializeContext context);
        public abstract bool Initialize(ref T instance, in DeserializeContext context);
        public abstract bool Clone(in T instance, out T clone, in CloneContext context);

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

        bool ISerializer.Clone(object instance, out object clone, in CloneContext context)
        {
            var casted = (T)instance;
            if (Clone(casted, out casted, context))
            {
                clone = casted;
                return true;
            }
            clone = default;
            return false;
        }
    }

    public static class Serializer
    {
        public static class Member
        {
            public static IMember<T> Field<T, TValue>(Field<T, TValue>.Getter get, Serializer<TValue> serializer = null) =>
                new Field<T, TValue>(get, serializer ?? Lazy<TValue>());

            public static IMember Reflection(FieldInfo field, ISerializer serializer = null) =>
                new Experiment.Reflection(field, serializer ?? Lazy(field.FieldType));

            public static IMember<T> Reflection<T>(FieldInfo field, ISerializer serializer = null) =>
                new Reflection<T>(field, serializer ?? Lazy(field.FieldType));

            public static IMember Reflection(PropertyInfo property, ISerializer serializer = null) =>
                new Experiment.Reflection(property, serializer ?? Lazy(property.PropertyType));

            public static IMember<T> Reflection<T>(PropertyInfo property, ISerializer serializer = null) =>
                new Reflection<T>(property, serializer ?? Lazy(property.PropertyType));
        }

        public static class Blittable
        {
            public static Serializer<(T[] items, int count)> Pair<T>() where T : unmanaged => new BlittablePair<T>();

            public static Serializer<T[]> Array<T>() where T : unmanaged => Reference(new BlittableArray<T>());
            public static ISerializer Array(Type type, int size)
            {
                if (TryInvoke(nameof(Array), type, out ISerializer value)) return value;
                return Reference(new BlittableArray(type, size));
            }

            public static Serializer<T> Object<T>() where T : unmanaged => Reference(new BlittableObject<T>());
            public static ISerializer Object(Type type, int size)
            {
                if (TryInvoke(nameof(Object), type, out ISerializer value)) return value;
                return Reference(new BlittableObject(type, size));
            }

            static bool TryInvoke<T>(string name, Type type, out T value, params object[] arguments) =>
                Serializer.TryInvoke(typeof(Blittable).StaticMethods(), name, type, out value, arguments);
        }

        public static class Reflection
        {
            public static Serializer<Assembly> Assembly() => Reference(new AbstractAssembly());
            public static Serializer<Module> Module() => Reference(new AbstractModule(Assembly()));
            public static Serializer<Type> Type() => Reference(new AbstractType(Module()));
            public static Serializer<MethodInfo> Method() => Reference(new AbstractMethod(Type()));
            public static Serializer<MemberInfo> Member() => Reference(new AbstractMember(Module()));
        }

        public static Serializer<string> String() => Reference(new ConcreteString());
        public static ISerializer Abstract(Type type) => new AbstractObject(type, (Reflection.Type(), Object(type)));

        public static ISerializer Array(Type type, ISerializer serializer = null) =>
            Reference(new ConcreteArray(type, serializer ?? Lazy(type)));
        public static Serializer<T[]> Array<T>(Serializer<T> serializer = null) =>
            Reference(new ConcreteArray<T>(serializer ?? Lazy<T>()));

        public static Serializer<T> Object<T>(params IMember<T>[] members) => Reference(new ConcreteObject<T>(members));
        public static Serializer<T> Object<T>()
        {
            var fields = typeof(T).InstanceFields();
            var members = fields.Select(field => Member.Reflection<T>(field)).ToArray();
            return Object<T>(members);
        }

        public static ISerializer Object(Type type, params IMember[] members) => Reference(new ConcreteObject(type, members));
        public static ISerializer Object(Type type)
        {
            var fields = type.InstanceFields();
            var members = fields.Select(field => Member.Reflection(field)).ToArray();
            return Object(type, members);
        }

        public static Serializer<T> Lazy<T>() => new Lazy<T>();
        public static ISerializer Lazy(Type type)
        {
            if (TryInvoke(nameof(Lazy), type, out ISerializer serializer)) return serializer;
            return new Lazy(type);
        }

        public static Serializer<T> Delegate<T>() where T : Delegate =>
            Reference(new ConcreteDelegate<T>(Reflection.Method(), Abstract(typeof(object))));
        public static ISerializer Delegate(Type type)
        {
            if (TryInvoke(nameof(Delegate), type, out ISerializer value)) return value;
            return new Reference(new ConcreteDelegate(type, Reflection.Method(), Abstract(typeof(object))));
        }

        public static Serializer<T> Reference<T>(Serializer<T> serializer) => new Reference<T>(serializer);
        public static ISerializer Reference(ISerializer serializer)
        {
            if (TryInvoke(nameof(Reference), serializer, out var value, serializer)) return value;
            return new Reference(serializer);
        }

        static bool TryGeneric(ISerializer serializer, out Type type)
        {
            if (serializer.GetType().Bases().TryFirst(@base => @base.IsGenericType && @base.GetGenericTypeDefinition() == typeof(Serializer<>), out var generic) &&
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
            TryInvoke(typeof(Serializer).StaticMethods(), name, type, out value, arguments);

        static bool TryInvoke<T>(MethodInfo[] methods, string name, Type type, out T value, params object[] arguments)
        {
            if (methods.TryFirst(current => current.IsGenericMethod && current.Name == name, out var method))
            {
                try
                {
                    value = (T)method.MakeGenericMethod(type).Invoke(null, arguments);
                    return true;
                }
                catch { }
            }
            value = default;
            return false;
        }
    }
}