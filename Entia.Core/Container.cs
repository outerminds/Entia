using System;
using System.Reflection;

namespace Entia.Core
{
    public interface ITrait { }
    public interface IImplementation<TTrait> where TTrait : ITrait, new() { }
    public interface IImplementation<T, TTrait> where TTrait : ITrait, new() { }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public sealed class ImplementationAttribute : PreserveAttribute
    {
        public readonly TypeData Type;
        public readonly TypeData Implementation;
        public readonly object[] Arguments;

        public ImplementationAttribute() : this(typeof(void), typeof(void)) { }
        public ImplementationAttribute(Type implementation, params object[] arguments) : this(typeof(void), implementation, arguments) { }
        public ImplementationAttribute(Type type, Type implementation, params object[] arguments)
        {
            Type = type;
            Implementation = implementation;
            Arguments = arguments;
        }
    }

    public sealed class Container
    {
        static readonly TypeMap<object, TypeMap<ITrait, ITrait>> _defaults = new TypeMap<object, TypeMap<ITrait, ITrait>>();

        public static bool TryDefault<T, TTrait>(out TTrait implementation) where TTrait : ITrait
        {
            var traits = _defaults.TryGet<T>(out var map) ? map : _defaults[typeof(T)] = new TypeMap<ITrait, ITrait>();
            var trait =
                traits.TryGet<TTrait>(out var value) ? value :
                traits[typeof(TTrait)] = Default(typeof(T), typeof(TTrait));
            if (trait is TTrait casted)
            {
                implementation = casted;
                return true;
            }
            implementation = default;
            return false;
        }

        public static bool TryDefault<TTrait>(Type type, out TTrait implementation) where TTrait : ITrait
        {
            var traits = _defaults.TryGet(type, out var map) ? map : _defaults[type] = new TypeMap<ITrait, ITrait>();
            var trait =
                traits.TryGet<TTrait>(out var value) ? value :
                traits[typeof(TTrait)] = Default(type, typeof(TTrait));
            if (trait is TTrait casted)
            {
                implementation = casted;
                return true;
            }
            implementation = default;
            return false;
        }

        public static bool TryDefault(Type type, Type trait, out ITrait implementation)
        {
            var traits = _defaults.TryGet(type, out var map) ? map : _defaults[type] = new TypeMap<ITrait, ITrait>();
            if (traits.TryGet(trait, out implementation)) return implementation != null;
            implementation = traits[trait] = Default(type, trait);
            return implementation != null;
        }

        static ITrait Default(Type type, Type trait)
        {
            var typeData = TypeUtility.GetData(type);
            foreach (var attribute in type.GetCustomAttributes<ImplementationAttribute>(true))
            {
                if (attribute.Implementation.Type.Is(trait, true, true))
                {
                    try
                    {
                        var concrete =
                            attribute.Implementation.Type.IsGenericTypeDefinition &&
                            typeData.Arguments.Length == attribute.Implementation.Arguments.Length ?
                            attribute.Implementation.Type.MakeGenericType(typeData.Arguments) : attribute.Implementation.Type;
                        return (ITrait)Activator.CreateInstance(concrete, attribute.Arguments);
                    }
                    catch { }
                }
            }

            foreach (var member in typeData.StaticMembers)
            {
                if (member.IsDefined(typeof(ImplementationAttribute), true))
                {
                    try
                    {
                        switch (member)
                        {
                            case Type nested when nested.Is(trait, true, true):
                                var generic = nested.IsGenericTypeDefinition ? nested.MakeGenericType(typeData.Arguments) : nested;
                                return (ITrait)Activator.CreateInstance(generic);
                            case FieldInfo field when field.FieldType.Is(trait, true, true):
                                return (ITrait)field.GetValue(null);
                            case PropertyInfo property when property.PropertyType.Is(trait, true, true):
                                return (ITrait)property.GetValue(null);
                            case MethodInfo method when method.ReturnType.Is(trait, true, true):
                                return (ITrait)method.Invoke(null, Array.Empty<object>());
                        }
                    }
                    catch { }
                }
            }

            foreach (var @interface in typeData.Interfaces)
            {
                try
                {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IImplementation<>))
                    {
                        var arguments = @interface.GetGenericArguments();
                        if (arguments[0].Is(trait, true, true))
                            return Activator.CreateInstance(arguments[0]) as ITrait;
                    }
                }
                catch { }
            }

            var traitData = TypeUtility.GetData(trait);
            foreach (var attribute in trait.GetCustomAttributes<ImplementationAttribute>(true))
            {
                if (type.Is(attribute.Type.Type, true, true) &&
                    attribute.Implementation.Type.Is(trait, true, true))
                {
                    try
                    {
                        var concrete =
                            typeData.Definition == attribute.Type.Type &&
                            attribute.Implementation.Type.IsGenericTypeDefinition &&
                            typeData.Arguments.Length == attribute.Implementation.Arguments.Length ?
                            attribute.Implementation.Type.MakeGenericType(typeData.Arguments) : attribute.Implementation.Type;
                        return (ITrait)Activator.CreateInstance(concrete, attribute.Arguments);
                    }
                    catch { }
                }
            }

            foreach (var @interface in traitData.Interfaces)
            {
                try
                {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IImplementation<,>))
                    {
                        var arguments = @interface.GetGenericArguments();
                        if (type.Is(arguments[0], true, true) && arguments[1].Is(trait, true, true))
                            return Activator.CreateInstance(arguments[1]) as ITrait;
                    }
                }
                catch { }
            }

            return default;
        }

        static Result<T> Failure<T>(Type type, Type trait) =>
            Result.Failure($"Could not find an implementation of trait '{trait.FullFormat()}' for type '{type.FullFormat()}'");

        public readonly Container Parent;
        readonly TypeMap<object, TypeMap<ITrait, ITrait>> _implementations = new TypeMap<object, TypeMap<ITrait, ITrait>>();

        public Container(Container parent = null) { Parent = parent; }

        public Result<ITrait> Get(Type type, Type trait) =>
            TryGet(type, trait, out var implementation) ? Result.Success(implementation) : Failure<ITrait>(type, trait);
        public Result<TTrait> Get<TTrait>(Type type) where TTrait : ITrait =>
            TryGet<TTrait>(type, out var implementation) ? Result.Success(implementation) : Failure<TTrait>(type, typeof(TTrait));
        public Result<TTrait> Get<T, TTrait>() where TTrait : ITrait =>
            TryGet<T, TTrait>(out var implementation) ? Result.Success(implementation) : Failure<TTrait>(typeof(T), typeof(TTrait));

        public bool TryGet(Type type, Type trait, out ITrait implementation)
        {
            if (_implementations.TryGet(type, out var traits) && traits.TryGet(trait, out implementation, true)) return true;
            return Parent == null ? TryDefault(type, trait, out implementation) : Parent.TryGet(type, trait, out implementation);
        }

        public bool TryGet<TTrait>(Type type, out TTrait implementation) where TTrait : ITrait
        {
            if (_implementations.TryGet(type, out var traits) && traits.TryGet<TTrait>(out var value, true))
            {
                implementation = (TTrait)value;
                return true;
            }
            return Parent == null ? TryDefault(type, out implementation) : Parent.TryGet<TTrait>(type, out implementation);
        }

        public bool TryGet<T, TTrait>(out TTrait implementation) where TTrait : ITrait
        {
            if (_implementations.TryGet<T>(out var traits) && traits.TryGet<TTrait>(out var value, true))
            {
                implementation = (TTrait)value;
                return true;
            }
            return Parent == null ? TryDefault<T, TTrait>(out implementation) : Parent.TryGet<T, TTrait>(out implementation);
        }

        public bool Set<T, TTrait>(TTrait implementation) where TTrait : ITrait
        {
            var traits = _implementations.TryGet<T>(out var map) ? map : _implementations[typeof(T)] = new TypeMap<ITrait, ITrait>();
            return traits.Set<TTrait>(implementation);
        }

        public bool Set<TTrait>(Type type, TTrait implementation) where TTrait : ITrait
        {
            var traits = _implementations.TryGet(type, out var map) ? map : _implementations[type] = new TypeMap<ITrait, ITrait>();
            return traits.Set<TTrait>(implementation);
        }

        public bool Set(Type type, ITrait implementation)
        {
            var traits = _implementations.TryGet(type, out var map) ? map : _implementations[type] = new TypeMap<ITrait, ITrait>();
            return traits.Set(implementation.GetType(), implementation);
        }

        public bool Has<T>() => _implementations.Has<T>();
        public bool Has(Type type) => _implementations.Has(type);
        public bool Has<T, TTrait>() where TTrait : ITrait =>
            _implementations.TryGet<T>(out var map) && map.TryGet<TTrait>(out var implementation) && implementation != null;
        public bool Has<TTrait>(Type type) where TTrait : ITrait =>
            _implementations.TryGet(type, out var map) && map.TryGet<TTrait>(out var implementation) && implementation != null;
        public bool Has(Type type, Type trait) =>
            _implementations.TryGet(type, out var map) && map.TryGet(trait, out var implementation) && implementation != null;

        public bool Remove<T, TTrait>() where TTrait : ITrait => _implementations.TryGet<T>(out var map) && map.Remove<TTrait>(true);
        public bool Remove<TTrait>(Type type) where TTrait : ITrait => _implementations.TryGet(type, out var map) && map.Remove<TTrait>(true);
        public bool Remove(Type type, Type trait) => _implementations.TryGet(type, out var map) && map.Remove(trait, true);

        public bool Clear<T>() => _implementations.Remove<T>();
        public bool Clear(Type type) => _implementations.Remove(type);
        public bool Clear() => _implementations.Clear();
    }
}