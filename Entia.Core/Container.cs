using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Entia.Core.Providers;

namespace Entia.Core
{
    public interface ITrait :
        IImplementation<TypeAttributeProvider>,
        IImplementation<TraitAttributeProvider>,
        IImplementation<TypeMemberProvider>,
        IImplementation<TypeInterfaceProvider>,
        IImplementation<TraitInterfaceProvider>
    { }
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

    namespace Providers
    {
        public interface IProvider : ITrait
        {
            IEnumerable<ITrait> Provide(Type type, Type trait);
        }

        public sealed class TypeAttributeProvider : IProvider
        {
            public IEnumerable<ITrait> Provide(Type type, Type trait)
            {
                var data = TypeUtility.GetData(type);
                foreach (var attribute in type.GetCustomAttributes<ImplementationAttribute>(true))
                {
                    if (attribute.Implementation.Type.Is(trait, true, true))
                    {
                        var concrete =
                            attribute.Implementation.Type.IsGenericTypeDefinition &&
                            data.Arguments.Length == attribute.Implementation.Arguments.Length ?
                            attribute.Implementation.Type.MakeGenericType(data.Arguments) : attribute.Implementation.Type;
                        yield return (ITrait)Activator.CreateInstance(concrete, attribute.Arguments);
                    }
                }
            }
        }

        public sealed class TraitAttributeProvider : IProvider
        {
            public IEnumerable<ITrait> Provide(Type type, Type trait)
            {
                var typeData = TypeUtility.GetData(type);
                var traitData = TypeUtility.GetData(trait);
                foreach (var attribute in trait.GetCustomAttributes<ImplementationAttribute>(true))
                {
                    if (type.Is(attribute.Type.Type, true, true) && attribute.Implementation.Type.Is(trait, true, true))
                    {
                        var concrete =
                            typeData.Definition == attribute.Type.Type &&
                            attribute.Implementation.Type.IsGenericTypeDefinition &&
                            typeData.Arguments.Length == attribute.Implementation.Arguments.Length ?
                            attribute.Implementation.Type.MakeGenericType(typeData.Arguments) : attribute.Implementation.Type;
                        yield return (ITrait)Activator.CreateInstance(concrete, attribute.Arguments);
                    }
                }
            }
        }

        public sealed class TypeMemberProvider : IProvider
        {
            public IEnumerable<ITrait> Provide(Type type, Type trait)
            {
                var data = TypeUtility.GetData(type);
                foreach (var member in data.StaticMembers)
                {
                    if (member.IsDefined(typeof(ImplementationAttribute), true))
                    {
                        switch (member)
                        {
                            case Type nested when nested.Is(trait, true, true):
                                var generic = nested.IsGenericTypeDefinition ? nested.MakeGenericType(data.Arguments) : nested;
                                yield return (ITrait)Activator.CreateInstance(generic);
                                break;
                            case FieldInfo field when field.FieldType.Is(trait, true, true):
                                yield return (ITrait)field.GetValue(null);
                                break;
                            case PropertyInfo property when property.PropertyType.Is(trait, true, true):
                                yield return (ITrait)property.GetValue(null);
                                break;
                            case MethodInfo method when method.ReturnType.Is(trait, true, true):
                                yield return (ITrait)method.Invoke(null, Array.Empty<object>());
                                break;
                        }
                    }
                }
            }
        }

        public sealed class TypeInterfaceProvider : IProvider
        {
            public IEnumerable<ITrait> Provide(Type type, Type trait)
            {
                var data = TypeUtility.GetData(type);
                foreach (var @interface in data.Interfaces)
                {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IImplementation<>))
                    {
                        var arguments = @interface.GetGenericArguments();
                        if (arguments[0].Is(trait, true, true))
                            yield return (ITrait)Activator.CreateInstance(arguments[0]);
                    }
                }
            }
        }

        public sealed class TraitInterfaceProvider : IProvider
        {
            public IEnumerable<ITrait> Provide(Type type, Type trait)
            {
                var data = TypeUtility.GetData(trait);
                foreach (var @interface in data.Interfaces)
                {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IImplementation<,>))
                    {
                        var arguments = @interface.GetGenericArguments();
                        if (type.Is(arguments[0], true, true) && arguments[1].Is(trait, true, true))
                            yield return (ITrait)Activator.CreateInstance(arguments[1]);
                    }
                }
            }
        }
    }

    public sealed class Container
    {
        public readonly struct Implementations<T> : IEnumerable<Implementations<T>.Enumerator, T> where T : ITrait
        {
            public struct Enumerator : IEnumerator<T>
            {
                public T Current => _index < _implementations.Length ?
                    (T)_implementations[_index] :
                    (T)_defaults[_index - _implementations.Length];
                object IEnumerator.Current => Current;

                ITrait[] _implementations;
                ITrait[] _defaults;
                int _count;
                int _index;

                public Enumerator(ITrait[] implementations, ITrait[] defaults)
                {
                    _implementations = implementations;
                    _defaults = defaults;
                    _count = implementations.Length + defaults.Length;
                    _index = -1;
                }

                public bool MoveNext() => ++_index < _count;
                public void Reset() => _index = -1;
                public void Dispose() { _implementations = default; _defaults = default; }
            }

            public int Count => _implementations.Length + _defaults.Length;
            public T this[int index] => index < _implementations.Length ?
                (T)_implementations[index] :
                (T)_defaults[index - _implementations.Length];

            readonly ITrait[] _implementations;
            readonly ITrait[] _defaults;

            public Implementations(ITrait[] implementations, ITrait[] defaults)
            {
                _implementations = implementations;
                _defaults = defaults;
            }

            public Enumerator GetEnumerator() => new Enumerator(_implementations, _defaults);
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        static readonly TypeMap<object, TypeMap<ITrait, ITrait[]>> _defaults = new TypeMap<object, TypeMap<ITrait, ITrait[]>>();

        public static bool TryDefault<T, TTrait>(out TTrait implementation) where TTrait : ITrait
        {
            if (Defaults<T, TTrait>().TryFirst(out var trait) && trait is TTrait casted)
            {
                implementation = casted;
                return true;
            }
            implementation = default;
            return false;
        }

        public static bool TryDefault<TTrait>(Type type, out TTrait implementation) where TTrait : ITrait
        {
            if (Defaults<TTrait>(type).TryFirst(out var trait) && trait is TTrait casted)
            {
                implementation = casted;
                return true;
            }
            implementation = default;
            return false;
        }

        public static bool TryDefault(Type type, Type trait, out ITrait implementation) =>
            Defaults(type, trait).TryFirst(out implementation);

        public static ITrait[] Defaults<T, TTrait>() where TTrait : ITrait
        {
            var traits = _defaults.TryGet<T>(out var map) ? map : _defaults[typeof(T)] = new TypeMap<ITrait, ITrait[]>();
            if (traits.TryGet<TTrait>(out var implementations)) return implementations;
            return traits[typeof(TTrait)] = CreateDefaults(typeof(T), typeof(TTrait));
        }

        public static ITrait[] Defaults<TTrait>(Type type) where TTrait : ITrait
        {
            var traits = _defaults.TryGet(type, out var map) ? map : _defaults[type] = new TypeMap<ITrait, ITrait[]>();
            if (traits.TryGet<TTrait>(out var implementations)) return implementations;
            return traits[typeof(TTrait)] = CreateDefaults(type, typeof(TTrait));
        }

        public static ITrait[] Defaults(Type type, Type trait)
        {
            var traits = _defaults.TryGet(type, out var map) ? map : _defaults[type] = new TypeMap<ITrait, ITrait[]>();
            if (traits.TryGet(trait, out var implementations)) return implementations;
            return traits[trait] = CreateDefaults(type, trait);
        }

        // static readonly TypeInterfaceProvider _typeProvider = new TypeInterfaceProvider();
        static ITrait[] CreateDefaults(Type type, Type trait)
        {
            // return Enumerable.Concat(
            //     _typeProvider.Provide(type, typeof(IProvider)),
            //     _typeProvider.Provide(trait, typeof(IProvider)))
            //     .OfType<IProvider>()
            //     .SelectMany(provider => provider.Provide(type, trait))
            //     .Some()
            //     .Distinct()
            //     .ToArray();
            // return Enumerable.Concat(type.Interfaces(), trait.Interfaces())
            //     .Where(@interface => @interface.Is(typeof(IImplementation<>), definition: true))
            //     .Select(@interface => @interface.GetGenericArguments()[0])
            //     .Where(argument => argument.Is<IProvider>())
            //     .Select(argument => (IProvider)Activator.CreateInstance(argument))
            //     .SelectMany(provider => provider.Provide(type, trait))
            //     .Some()
            //     .Distinct()
            //     .ToArray();

            var implementations = new List<ITrait>();
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
                        implementations.Add((ITrait)Activator.CreateInstance(concrete, attribute.Arguments));
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
                                implementations.Add((ITrait)Activator.CreateInstance(generic));
                                break;
                            case FieldInfo field when field.FieldType.Is(trait, true, true):
                                implementations.Add((ITrait)field.GetValue(null));
                                break;
                            case PropertyInfo property when property.PropertyType.Is(trait, true, true):
                                implementations.Add((ITrait)property.GetValue(null));
                                break;
                            case MethodInfo method when method.ReturnType.Is(trait, true, true):
                                implementations.Add((ITrait)method.Invoke(null, Array.Empty<object>()));
                                break;
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
                            implementations.Add((ITrait)Activator.CreateInstance(arguments[0]));
                    }
                }
                catch { }
            }

            var traitData = TypeUtility.GetData(trait);
            foreach (var attribute in trait.GetCustomAttributes<ImplementationAttribute>(true))
            {
                if (type.Is(attribute.Type.Type, true, true) && attribute.Implementation.Type.Is(trait, true, true))
                {
                    try
                    {
                        var concrete =
                            typeData.Definition == attribute.Type.Type &&
                            attribute.Implementation.Type.IsGenericTypeDefinition &&
                            typeData.Arguments.Length == attribute.Implementation.Arguments.Length ?
                            attribute.Implementation.Type.MakeGenericType(typeData.Arguments) : attribute.Implementation.Type;
                        implementations.Add((ITrait)Activator.CreateInstance(concrete, attribute.Arguments));
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
                            implementations.Add((ITrait)Activator.CreateInstance(arguments[1]));
                    }
                }
                catch { }
            }

            return implementations.Some().Distinct().ToArray();
        }

        readonly TypeMap<object, TypeMap<ITrait, ITrait[]>> _implementations = new TypeMap<object, TypeMap<ITrait, ITrait[]>>();

        public Container() { }

        public Implementations<ITrait> Get(Type type, Type trait) =>
            new Implementations<ITrait>(GetImplementations(type, trait), Defaults(type, trait));
        public Implementations<TTrait> Get<TTrait>(Type type) where TTrait : ITrait =>
            new Implementations<TTrait>(GetImplementations<TTrait>(type), Defaults<TTrait>(type));
        public Implementations<TTrait> Get<T, TTrait>() where TTrait : ITrait =>
            new Implementations<TTrait>(GetImplementations<T, TTrait>(), Defaults<T, TTrait>());

        public bool TryGet(Type type, Type trait, out ITrait implementation)
        {
            if (_implementations.TryGet(type, out var traits) &&
                traits.TryGet(trait, out var implementations) &&
                implementations.TryFirst(out implementation))
                return true;
            return TryDefault(type, trait, out implementation);
        }
        public bool TryGet<TTrait>(Type type, out TTrait implementation) where TTrait : ITrait
        {
            if (_implementations.TryGet(type, out var traits) &&
                traits.TryGet<TTrait>(out var implementations) &&
                implementations.TryFirst(out var value))
            {
                implementation = (TTrait)value;
                return true;
            }
            return TryDefault<TTrait>(type, out implementation);
        }
        public bool TryGet<T, TTrait>(out TTrait implementation) where TTrait : ITrait
        {
            if (_implementations.TryGet<T>(out var traits) &&
                traits.TryGet<TTrait>(out var implementations) &&
                implementations.TryFirst(out var value))
            {
                implementation = (TTrait)value;
                return true;
            }
            return TryDefault<T, TTrait>(out implementation);
        }

        public bool Has<T>() => _implementations.Has<T>();
        public bool Has(Type type) => _implementations.Has(type);
        public bool Has<T, TTrait>() where TTrait : ITrait =>
            _implementations.TryGet<T>(out var map) &&
            map.TryGet<TTrait>(out var implementations) &&
            implementations.Length > 0;
        public bool Has<TTrait>(Type type) where TTrait : ITrait =>
            _implementations.TryGet(type, out var map) &&
            map.TryGet<TTrait>(out var implementations) &&
            implementations.Length > 0;
        public bool Has(Type type, Type trait) =>
            _implementations.TryGet(type, out var map) &&
            map.TryGet(trait, out var implementations) &&
            implementations.Length > 0;

        public void Add<T, TTrait>(TTrait implementation) where TTrait : ITrait =>
            ArrayUtility.Add(ref GetImplementations<T, TTrait>(), implementation);
        public void Add<TTrait>(Type type, TTrait implementation) where TTrait : ITrait =>
            ArrayUtility.Add(ref GetImplementations<TTrait>(type), implementation);
        public void Add(Type type, ITrait implementation) =>
            ArrayUtility.Add(ref GetImplementations(type, implementation.GetType()), implementation);

        public bool Remove<T, TTrait>(TTrait implementation) where TTrait : ITrait =>
            ArrayUtility.Remove(ref GetImplementations<T, TTrait>(), implementation);
        public bool Remove<TTrait>(Type type, TTrait implementation) where TTrait : ITrait =>
            ArrayUtility.Remove(ref GetImplementations<TTrait>(type), implementation);
        public bool Remove(Type type, ITrait implementation) =>
            ArrayUtility.Remove(ref GetImplementations(type, implementation.GetType()), implementation);

        public bool Clear<T, TTrait>() where TTrait : ITrait
        {
            ref var implementations = ref GetImplementations<T, TTrait>();
            if (implementations.Length > 0)
            {
                implementations = Array.Empty<ITrait>();
                return true;
            }
            return false;
        }
        public bool Clear<TTrait>(Type type) where TTrait : ITrait
        {
            ref var implementations = ref GetImplementations<TTrait>(type);
            if (implementations.Length > 0)
            {
                implementations = Array.Empty<ITrait>();
                return true;
            }
            return false;
        }
        public bool Clear(Type type, Type trait)
        {
            ref var implementations = ref GetImplementations(type, trait);
            if (implementations.Length > 0)
            {
                implementations = Array.Empty<ITrait>();
                return true;
            }
            return false;
        }
        public bool Clear<T>() => _implementations.Remove<T>();
        public bool Clear(Type type) => _implementations.Remove(type);
        public bool Clear() => _implementations.Clear();

        ref ITrait[] GetImplementations(Type type, Type trait)
        {
            var traits =
                _implementations.TryGet(type, out var map) ? map :
                _implementations[type] = new TypeMap<ITrait, ITrait[]>();
            if (traits.TryIndex(trait, out var index)) return ref GetImplementations(index, traits);
            throw new ArgumentException(nameof(trait));
        }

        ref ITrait[] GetImplementations<TTrait>(Type type) where TTrait : ITrait
        {
            var traits =
                _implementations.TryGet(type, out var map) ? map :
                _implementations[type] = new TypeMap<ITrait, ITrait[]>();
            return ref GetImplementations(traits.Index<TTrait>(), traits);
        }

        ref ITrait[] GetImplementations<T, TTrait>() where TTrait : ITrait
        {
            var traits =
                _implementations.TryGet<T>(out var map) ? map :
                _implementations[typeof(T)] = new TypeMap<ITrait, ITrait[]>();
            return ref GetImplementations(traits.Index<TTrait>(), traits);
        }

        ref ITrait[] GetImplementations(int index, TypeMap<ITrait, ITrait[]> traits)
        {
            if (traits.Has(index)) return ref traits[index];
            traits.Set(index, Array.Empty<ITrait>());
            return ref traits[index];
        }
    }
}