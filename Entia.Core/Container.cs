using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Entia.Core.Documentation;
using Entia.Core.Providers;

namespace Entia.Core
{
    namespace Providers
    {
        /// <summary>
        /// Interface that provides implementations for a given type.
        /// </summary>
        public interface IProvider : ITrait { IEnumerable<ITrait> Provide(TypeData type, TypeData trait); }
        /// <summary>
        /// Interface that provides implementations of a trait 'T' for a given type.
        /// </summary>
        public abstract class Provider<T> : IProvider where T : ITrait
        {
            public abstract IEnumerable<T> Provide(TypeData type);
            IEnumerable<ITrait> IProvider.Provide(TypeData type, TypeData trait) =>
                typeof(T).Is(trait, true, true) ? Provide(type).Cast<ITrait>() : Array.Empty<ITrait>();
        }
    }

    /// <summary>
    /// Interface that all traits must implement.
    /// </summary>
    public interface ITrait { }
    /// <summary>
    /// Interface that links an implementing type with a trait implementation 'TTrait'.
    /// </summary>
    public interface IImplementation<TTrait> where TTrait : ITrait, new() { }
    /// <summary>
    /// Interface that links a type 'T' with a trait implementation 'TTrait'. Must be implemented a trait interface.
    /// </summary>
    public interface IImplementation<T, TTrait> where TTrait : ITrait, new() { }

    /// <summary>
    /// Attribute that links a type with a trait implementation. This attribute can be used in the following scenarios:
    /// - can be applied to a type by specifying the trait implementation type
    /// - can be applied to a static field/property/method of a type that provides a trait implementation
    /// - can be applied to a sub type of a type that implements a trait implementation
    /// - can be applied to a trait interface by specifying a type and an implementation type
    /// </summary>
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

    /// <summary>
    /// Links any type with its trait implementations. A trait is essentially an extension interface, meaning that
    /// it can be considered as an interface that can be defined for types that are not owned by their consumer. The
    /// container stores and retrieves the implementations for a given type. It also retrieves default implementations
    /// that can be linked using the 'IImplementation' interface or the '[Implementation]' attribute.
    /// </summary>
    public sealed class Container : IEnumerable<(Type type, Type trait, ITrait implementation)>
    {
        public readonly struct Implementations<T> : IEnumerable<Implementations<T>.Enumerator, T> where T : ITrait
        {
            public struct Enumerator : IEnumerator<T>
            {
                public T Current => _index < _implementations.Length ?
                    (T)_implementations[_index] :
                    (T)_defaults[_index - _implementations.Length];
                object IEnumerator.Current => Current;

                readonly ITrait[] _implementations;
                readonly ITrait[] _defaults;
                readonly int _count;
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
                public void Dispose() => this = default;
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

        [ThreadSafe]
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

        [ThreadSafe]
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

        [ThreadSafe]
        public static bool TryDefault(Type type, Type trait, out ITrait implementation) =>
            Defaults(type, trait).TryFirst(out implementation);

        [ThreadSafe]
        public static ITrait[] Defaults<T, TTrait>() where TTrait : ITrait
        {
            var traits = GetTraits(_defaults.Index<T>());
            return GetDefaults(traits, TypeUtility.GetData<T>(), (TypeUtility.GetData<TTrait>(), traits.Index<TTrait>()));
        }

        [ThreadSafe]
        public static ITrait[] Defaults<TTrait>(Type type) where TTrait : ITrait
        {
            if (_defaults.TryIndex(type, out var typeIndex) && GetTraits(typeIndex) is var traits)
                return GetDefaults(traits, type, (TypeUtility.GetData<TTrait>(), traits.Index<TTrait>()));
            return Array.Empty<ITrait>();
        }

        [ThreadSafe]
        public static ITrait[] Defaults(Type type, Type trait)
        {
            if (_defaults.TryIndex(type, out var typeIndex) &&
                GetTraits(typeIndex) is var traits &&
                traits.TryIndex(trait, out var traitIndex))
                return GetDefaults(traits, type, (trait, traitIndex));
            return Array.Empty<ITrait>();
        }

        [ThreadSafe]
        static TypeMap<ITrait, ITrait[]> GetTraits(int index)
        {
            if (_defaults.TryGet(index, out var traits)) return traits;
            lock (_defaults)
            {
                if (_defaults.TryGet(index, out traits)) return traits;
                _defaults.Set(index, traits = new TypeMap<ITrait, ITrait[]>());
                return traits;
            }
        }

        [ThreadSafe]
        static ITrait[] GetDefaults(TypeMap<ITrait, ITrait[]> traits, TypeData type, (TypeData type, int index) trait)
        {
            if (traits.TryGet(trait.index, out var defaults)) return defaults;
            lock (traits)
            {
                if (traits.TryGet(trait.index, out defaults)) return defaults;
                traits.Set(trait.index, defaults = CreateDefaults(type, trait.type));
                return defaults;
            }
        }

        [ThreadSafe]
        static ITrait[] CreateDefaults(TypeData type, TypeData trait)
        {
            static bool Is(Type current, Type other) => current.Is<IProvider>() || current.Is(other, true, true);

            static Type Concrete(TypeData data, ImplementationAttribute attribute)
            {
                var implementation = attribute.Implementation;
                if (implementation.Type.IsGenericTypeDefinition)
                {
                    if (attribute.Type is TypeData)
                    {
                        if (attribute.Type.Type.IsGenericTypeDefinition &&
                            attribute.Type.Arguments.Length == implementation.Arguments.Length &&
                            data.Type.Hierarchy().TryFirst(child =>
                                child.IsGenericType && child.GetGenericTypeDefinition() == attribute.Type, out var definition))
                            return implementation.Type.MakeGenericType(definition.GetGenericArguments());
                        return implementation.Type.MakeGenericType(data);
                    }
                    else if (data.Arguments.Length == implementation.Arguments.Length)
                        return implementation.Type.MakeGenericType(data.Arguments);
                }
                return implementation.Type;
            }

            IEnumerable<Option<ITrait>> Create()
            {
                foreach (var attribute in type.Type.GetCustomAttributes<ImplementationAttribute>(true))
                {
                    if (Is(attribute.Implementation.Type, trait))
                        yield return GetInstance(Concrete(type, attribute), attribute.Arguments);
                }

                foreach (var member in type.StaticMembers.Values)
                {
                    foreach (var attribute in member.GetCustomAttributes<ImplementationAttribute>(true))
                    {
                        switch (member)
                        {
                            case Type nested when Is(nested, trait):
                                var generic = nested.IsGenericTypeDefinition ? nested.MakeGenericType(type.Arguments) : nested;
                                yield return GetInstance(generic, attribute.Arguments);
                                break;
                            case FieldInfo field when Is(field.FieldType, trait):
                                yield return Option.Cast<ITrait>(field.GetValue(null));
                                break;
                            case PropertyInfo property when Is(property.PropertyType, trait):
                                yield return Option.Cast<ITrait>(property.GetValue(null));
                                break;
                            case MethodInfo method when Is(method.ReturnType, trait):
                                yield return Option.Cast<ITrait>(method.Invoke(null, attribute.Arguments));
                                break;
                        }
                    }
                }

                foreach (var @interface in type.Interfaces)
                {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IImplementation<>))
                    {
                        var arguments = @interface.GetGenericArguments();
                        if (Is(arguments[0], trait)) yield return GetInstance(arguments[0]);
                    }
                }

                foreach (var attribute in trait.Type.GetCustomAttributes<ImplementationAttribute>(true))
                {
                    if (type.Type.Is(attribute.Type.Type, true, true) && Is(attribute.Implementation.Type, trait))
                        yield return GetInstance(Concrete(type, attribute), attribute.Arguments);
                }

                foreach (var @interface in trait.Interfaces)
                {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IImplementation<,>))
                    {
                        var arguments = @interface.GetGenericArguments();
                        if (type.Type.Is(arguments[0], true, true) && Is(arguments[1], trait))
                            yield return GetInstance(arguments[1]);
                    }
                }
            }

            IEnumerable<ITrait> Flatten(ITrait instance)
            {
                yield return instance;
                if (instance is IProvider provider)
                {
                    foreach (var implementation in provider.Provide(type, trait))
                        foreach (var inner in Flatten(implementation)) yield return inner;
                }
            }

            return Create()
                .Choose()
                .SelectMany(Flatten)
                .OfType(trait, true, true)
                .Distinct()
                .ToArray();
        }

        static Option<ITrait> GetInstance(Type type, params object[] arguments) =>
            Option.Cast<ITrait>(Activator.CreateInstance(type, arguments));

        readonly TypeMap<object, TypeMap<ITrait, ITrait[]>> _implementations = new TypeMap<object, TypeMap<ITrait, ITrait[]>>();

        public Container() { }

        public Implementations<ITrait> Get(Type type, Type trait) =>
            new Implementations<ITrait>(GetImplementations(type, trait), Defaults(type, trait));
        public Implementations<TTrait> Get<TTrait>(Type type) where TTrait : ITrait =>
            new Implementations<TTrait>(GetImplementations<TTrait>(type), Defaults<TTrait>(type));
        public Implementations<TTrait> Get<T, TTrait>() where TTrait : ITrait =>
            new Implementations<TTrait>(GetImplementations<T, TTrait>(), Defaults<T, TTrait>());

        [ThreadSafe]
        public bool TryGet(Type type, Type trait, out ITrait implementation)
        {
            if (_implementations.TryGet(type, out var traits, true, false) &&
                traits.TryGet(trait, out var implementations) &&
                implementations.TryFirst(out implementation))
                return true;
            return TryDefault(type, trait, out implementation);
        }

        [ThreadSafe]
        public bool TryGet<TTrait>(Type type, out TTrait implementation) where TTrait : ITrait
        {
            if (_implementations.TryGet(type, out var traits, true, false) &&
                traits.TryGet<TTrait>(out var implementations) &&
                implementations.TryFirst(out var value))
            {
                implementation = (TTrait)value;
                return true;
            }
            return TryDefault(type, out implementation);
        }

        [ThreadSafe]
        public bool TryGet<T, TTrait>(out TTrait implementation) where TTrait : ITrait
        {
            if (_implementations.TryGet<T>(out var traits, true, false) &&
                traits.TryGet<TTrait>(out var implementations) &&
                implementations.TryFirst(out var value))
            {
                implementation = (TTrait)value;
                return true;
            }
            return TryDefault<T, TTrait>(out implementation);
        }

        [ThreadSafe]
        public bool Has<T>() => _implementations.Has<T>(true, false);
        [ThreadSafe]
        public bool Has(Type type) => _implementations.Has(type, true, false);
        [ThreadSafe]
        public bool Has<T, TTrait>() where TTrait : ITrait =>
            _implementations.TryGet<T>(out var map, true, false) &&
            map.TryGet<TTrait>(out var implementations) &&
            implementations.Length > 0;
        [ThreadSafe]
        public bool Has<TTrait>(Type type) where TTrait : ITrait =>
            _implementations.TryGet(type, out var map, true, false) &&
            map.TryGet<TTrait>(out var implementations) &&
            implementations.Length > 0;
        [ThreadSafe]
        public bool Has(Type type, Type trait) =>
            _implementations.TryGet(type, out var map, true, false) &&
            map.TryGet(trait, out var implementations) &&
            implementations.Length > 0;

        public void Add<T, TTrait>(TTrait implementation) where TTrait : ITrait =>
            ArrayUtility.Append(ref GetImplementations<T, TTrait>(), implementation);
        public void Add<TTrait>(Type type, TTrait implementation) where TTrait : ITrait =>
            ArrayUtility.Append(ref GetImplementations<TTrait>(type), implementation);
        public void Add(Type type, ITrait implementation) => Add(type, implementation.GetType(), implementation);
        public void Add(Type type, Type trait, ITrait implementation) =>
            ArrayUtility.Append(ref GetImplementations(type, trait), implementation);

        public bool Remove<T, TTrait>(TTrait implementation) where TTrait : ITrait =>
            ArrayUtility.Remove(ref GetImplementations<T, TTrait>(), implementation);
        public bool Remove<TTrait>(Type type, TTrait implementation) where TTrait : ITrait =>
            ArrayUtility.Remove(ref GetImplementations<TTrait>(type), implementation);
        public bool Remove(Type type, ITrait implementation) => Remove(type, implementation.GetType(), implementation);
        public bool Remove(Type type, Type trait, ITrait implementation) =>
            ArrayUtility.Remove(ref GetImplementations(type, trait), implementation);

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
                _implementations.TryGet(type, out var map, true, false) ? map :
                _implementations[type] = new TypeMap<ITrait, ITrait[]>();
            if (traits.TryIndex(trait, out var index)) return ref GetImplementations(index, traits);
            throw new ArgumentException(nameof(trait));
        }

        ref ITrait[] GetImplementations<TTrait>(Type type) where TTrait : ITrait
        {
            var traits =
                _implementations.TryGet(type, out var map, true, false) ? map :
                _implementations[type] = new TypeMap<ITrait, ITrait[]>();
            return ref GetImplementations(traits.Index<TTrait>(), traits);
        }

        ref ITrait[] GetImplementations<T, TTrait>() where TTrait : ITrait
        {
            var traits =
                _implementations.TryGet<T>(out var map, true, false) ? map :
                _implementations[typeof(T)] = new TypeMap<ITrait, ITrait[]>();
            return ref GetImplementations(traits.Index<TTrait>(), traits);
        }

        ref ITrait[] GetImplementations(int index, TypeMap<ITrait, ITrait[]> traits)
        {
            if (traits.Has(index)) return ref traits[index];
            traits.Set(index, Array.Empty<ITrait>());
            return ref traits[index];
        }

        [ThreadSafe]
        public IEnumerator<(Type type, Type trait, ITrait implementation)> GetEnumerator()
        {
            foreach (var (type, map) in _implementations)
                foreach (var (trait, implementations) in map)
                    foreach (var implementation in implementations)
                        yield return (type, trait, implementation);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}