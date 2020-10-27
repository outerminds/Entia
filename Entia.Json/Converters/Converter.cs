using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Entia.Core;

namespace Entia.Json.Converters
{
    /// <summary>
    /// Attribute that can be used to provide default implementations of an <see cref="IConverter"/>
    /// to the library statically without having to pass them explicitly through <see cref="Settings"/>.
    /// <para>
    /// The attribute can be applied to a static field/property or parameterless method/constructor that
    /// provides an <see cref="IConverter"/> instance that covers the declaring type such that this type
    /// is assignable from the <see cref="IConverter.Type"/> property (works with generic definitions).
    /// </para>
    /// </summary>
    /// /// <remarks>
    /// This attribute can only be used for types that are defined by the user. To handle other types,
    /// see <see cref="IConverter"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public sealed class ConverterAttribute : PreserveAttribute { }

    /// <summary>
    /// Interface that can be implemented to to provide custom conversion from and to a
    /// <see cref="Node"/>. Instances of this interface must be passed to calls to the
    /// library through the <see cref="Settings"/> parameter. This is the main point of extensibility
    /// of the library.
    /// <para>
    /// When converting from or to a <see cref="Node"/>, the <see cref="IConverter.Type"/> property will be used
    /// to find the proper converter for a given value. The <see cref="IConverter.Type"/> property
    /// may provide an abstract type or a generic definition to signify that all derived concrete
    /// types are supported.
    /// Note that certain exceptions apply. Primitives, enums, strings and null values will
    /// never make it to a converter, thus their conversion cannot be overridden. For any other type,
    /// including the ones for which a default converter is provided by the library, the conversion
    /// can be overridden by using <see cref="Settings"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// When possible, it is recommended to use the more type safe alternative <see cref="Converter{T}"/>.
    /// </remarks>
    public interface IConverter
    {
        Type Type { get; }
        Node Convert(in ToContext context);
        object Instantiate(in FromContext context);
        void Initialize(ref object instance, in FromContext context);
    }

    /// <summary>
    /// A type safe alternative to <see cref="IConverter"/>.
    /// </summary>
    /// <typeparam name="T">The type to convert from and to a <see cref="Node"/>.</typeparam>
    public abstract class Converter<T> : IConverter
    {
        /// <inheritdoc/>
        public virtual Type Type => typeof(T);
        /// <inheritdoc cref="IConverter.Convert"/>
        public abstract Node Convert(in T instance, in ToContext context);
        /// <inheritdoc cref="IConverter.Instantiate"/>
        public abstract T Instantiate(in FromContext context);
        /// <inheritdoc cref="IConverter.Initialize"/>
        public virtual void Initialize(ref T instance, in FromContext context) { }

        Node IConverter.Convert(in ToContext context) =>
            context.Instance is T casted ? Convert(casted, context) : Node.Null;
        object IConverter.Instantiate(in FromContext context) => Instantiate(context);
        void IConverter.Initialize(ref object instance, in FromContext context)
        {
            if (instance is T casted)
            {
                Initialize(ref casted, context);
                instance = casted;
            }
        }
    }

    /// <summary>
    /// Module that exposes default <see cref="IConverter"/> instances and converter constructors.
    /// </summary>
    public static class Converter
    {
        public delegate Option<(int version, Node node)> Upgrade(Node node);
        public delegate bool Validate(Type type);
        public delegate Node Convert<T>(in T instance, in ToContext context);
        public delegate T Instantiate<T>(in FromContext context);
        public delegate void Initialize<T>(ref T instance, in FromContext context);

        sealed class Function<T> : Converter<T>
        {
            readonly Convert<T> _convert;
            readonly Instantiate<T> _instantiate;
            readonly Initialize<T> _initialize;

            public Function(Convert<T> convert = null, Instantiate<T> instantiate = null, Initialize<T> initialize = null)
            {
                _convert = convert ?? ((in T instance, in ToContext context) => Cache<T>.Default.Convert(context));
                _instantiate = instantiate ?? ((in FromContext context) => (T)Cache<T>.Default.Instantiate(context));
                _initialize = initialize ?? ((ref T instance, in FromContext context) =>
                {
                    var box = (object)instance;
                    Cache<T>.Default.Initialize(ref box, context);
                    instance = (T)box;
                });
            }

            public override Node Convert(in T instance, in ToContext context) => _convert(instance, context);
            public override T Instantiate(in FromContext context) => _instantiate(context);
            public override void Initialize(ref T instance, in FromContext context) => _initialize(ref instance, context);
        }

        static class Cache<T>
        {
            public static readonly IConverter Default = Converter.Default(typeof(T));
        }

        static readonly ConcurrentDictionary<Type, IConverter> _converters = new ConcurrentDictionary<Type, IConverter>();
        static readonly ConcurrentDictionary<Type, IConverter> _defaults = new ConcurrentDictionary<Type, IConverter>();

        /// <summary>
        /// Provides a default <see cref="IConverter"/> instance for the provided type.
        /// This instance may be a library built-in one or one that has been linked with the
        /// <see cref="ConverterAttribute"/> attribute.
        /// </summary>
        public static IConverter Default(Type type) => _converters.GetOrAdd(type, key =>
            CreateAttribute(key).TryValue(out var converter) ? converter : GetDefault(key));
        /// <inheritdoc cref="Default(Type)"/>
        public static IConverter Default<T>() => Cache<T>.Default;

        /// <summary>
        /// Creates a <see cref="Converter{T}"/> instance for type <typeparamref name="TSource"/> that
        /// wraps a conversion from <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>.
        /// If no <see cref="Converter{T}"/> instance for type <typeparamref name="TTarget"/> is provided,
        /// the default one will be used.
        /// </summary>
        public static Converter<TSource> Create<TSource, TTarget>(InFunc<TSource, TTarget> to, InFunc<TTarget, TSource> from, Converter<TTarget> converter = null) =>
            Create(
                (in TSource instance, in ToContext context) => context.Convert(to(instance), converter, converter),
                (in FromContext context) => from(context.Convert<TTarget>(context.Node, converter, converter)));

        /// <summary>
        /// Creates a <see cref="Converter{T}"/> instance with the provided conversions from and to
        /// a <see cref="Node"/>.
        /// </summary>
        public static Converter<T> Create<T>(InFunc<T, Node> to, Func<Node, T> from) => Create(
            (in T instance, in ToContext context) => to(instance),
            (in FromContext context) => from(context.Node));

        /// <summary>
        /// Creates a <see cref="Converter{T}"/> instance with the provided conversions from and to
        /// a <see cref="Node"/>.
        /// </summary>
        public static Converter<T> Create<T>(Convert<T> convert = null, Instantiate<T> instantiate = null, Initialize<T> initialize = null) =>
            new Function<T>(convert, instantiate, initialize);

        /// <inheritdoc cref="Version{T}(int, int, ValueTuple{int, Converter{T}}[])"/>
        /// <remarks>
        /// The lowest version is considered to be the default version and the largest version
        /// is considered to be the latest.
        /// </remarks>
        public static Converter<T> Version<T>(params (int version, Converter<T> converter)[] converters) =>
            Version(converters.Min(pair => pair.version), converters.Max(pair => pair.version), converters);
        /// <summary>
        /// Creates a <see cref="Converter{T}"/> instance that selects one of the given converters
        /// based on a version. This converter also wraps the json in an object that holds the version
        /// in the following format: <code>{ $k: version, $v: json }</code>
        /// <list type="bullet">
        /// <item>
        /// When converting to a <see cref="Node"/>, the converter with the <paramref name="latest"/>
        /// version will be used.
        /// <item>
        /// </item>
        /// When converting from a <see cref="Node"/>, the converter with the corresponding version
        /// will be used. If the version does not match any converter or is absent, the converter
        /// with the <paramref name="default"/> version will be used.
        /// </item>
        /// </list>
        /// </summary>
        public static Converter<T> Version<T>(int @default, int latest, params (int version, Converter<T> converter)[] converters)
        {
            var versionToConverter = converters.ToDictionary(pair => pair.version, pair => pair.converter);
            var defaultConverter = versionToConverter[@default];
            var latestConverter = versionToConverter[latest];

            Converter<T> Converter(Node node, out Node value)
            {
                var pair =
                    node.IsObject() && node.Children.Length == 4 &&
                    node.Children[0] == Node.DollarKString && node.Children[2] == Node.DollarVString ?
                    (version: node.Children[1].AsInt(), value: node.Children[3]) :
                    (version: @default, value: node);
                value = pair.value;
                return versionToConverter.TryGetValue(pair.version, out var converter) ? converter : defaultConverter;
            }

            return Create(
                (in T instance, in ToContext context) =>
                    Node.Object(Node.DollarKString, latest, Node.DollarVString, latestConverter.Convert(instance, context)),
                (in FromContext context) =>
                    Converter(context.Node, out var value).Instantiate(context.With(value)),
                (ref T instance, in FromContext context) =>
                    Converter(context.Node, out var value).Initialize(ref instance, context.With(value)));
        }

        /// <summary>
        /// Creates a <see cref="Converter{T}"/> instance that selects either the
        /// <paramref name="true"/> or <paramref name="false"/> converter based on the provided
        /// <paramref name="condition"/>.
        /// <para>
        /// For example, this is especially useful for cases where the same value may have different
        /// json representations. A value of type 'Vector2' may be converted to an object with format:
        /// <code>{ "X": 1, "Y": 2 }</code> or may be converted to an array with format:
        /// <code>[1, 2]</code>
        /// In this case, the condition allows to select
        /// a converter based on wether the a <see cref="Node"/> is an object or an array.
        /// </para>
        /// </summary>
        public static Converter<T> If<T>((InFunc<T, bool> to, Func<Node, bool> from) condition, Converter<T> @true, Converter<T> @false)
        {
            condition.to ??= (in T _) => true;
            condition.from ??= _ => true;
            return Create(
                (in T instance, in ToContext context) =>
                    condition.to(instance) ? @true.Convert(instance, context) : @false.Convert(instance, context),
                (in FromContext context) =>
                    condition.from(context.Node) ? @true.Instantiate(context) : @false.Instantiate(context),
                (ref T instance, in FromContext context) =>
                {
                    if (condition.from(context.Node))
                        @true.Initialize(ref instance, context);
                    else
                        @false.Initialize(ref instance, context);
                }
            );
        }
        /// <inheritdoc cref="If{T}(ValueTuple{InFunc{T, bool}, Func{Node, bool}}, Converter{T}, Converter{T})"/>
        public static Converter<T> If<T>(InFunc<T, bool> condition, Converter<T> @true, Converter<T> @false) => If((condition, null), @true, @false);
        /// <inheritdoc cref="If{T}(ValueTuple{InFunc{T, bool}, Func{Node, bool}}, Converter{T}, Converter{T})"/>
        public static Converter<T> If<T>(Func<Node, bool> condition, Converter<T> @true, Converter<T> @false) => If((null, condition), @true, @false);

        /// <summary>
        /// Creates a <see cref="Converter{T}"/> instance that will convert values of type
        /// <typeparam name="T"/> to an object representation described by the provided
        /// <paramref name="members"/>.
        /// <para>
        /// <example>
        /// For example, the converter:
        /// <code>
        /// Object(
        ///     (in FromContext _) => new Vector2()),
        ///     Member.Field(nameof(Vector2.X), (in Vector2 value) => ref value.X),
        ///     Member.Field(nameof(Vector2.Y), (in Vector2 value) => ref value.Y));
        /// </code>
        /// will convert a value of type 'Vector2' to an object with format:
        /// <code>{ "X": 1, "Y": 2 }</code>
        /// </example>
        /// </para>
        /// </summary>
        /// <remarks>
        /// An explicit converter implementation like one returned by this function should be
        /// much more performant than the default reflection-based converter.
        /// </remarks>
        public static Converter<T> Object<T>(Instantiate<T> instantiate = null, params Member<T>[] members)
        {
            var map = members
                .SelectMany(member => member.Aliases.Prepend(member.Name).Select(name => (member, name)))
                .ToDictionary(pair => pair.name, pair => pair.member);
            return Create(
                (in T instance, in ToContext context) =>
                {
                    var children = new List<Node>(members.Length * 2);
                    for (int i = 0; i < members.Length; i++)
                    {
                        var member = members[i];
                        if (member.Convert(instance, context) is Node node)
                        {
                            children.Add(member.Key);
                            children.Add(node);
                        }
                    }
                    return Node.Object(children.ToArray());
                },
                instantiate,
                (ref T instance, in FromContext context) =>
                {
                    foreach (var (key, value) in context.Node.Members())
                    {
                        if (map.TryGetValue(key, out var member))
                            member.Initialize(ref instance, context.With(value));
                    }
                }
            );
        }

        /// <summary>
        /// Creates a <see cref="Converter{T}"/> instance that will convert values of type
        /// <typeparam name="T"/> to an array representation described by the provided
        /// <paramref name="items"/>.
        /// <para>
        /// <example>
        /// For example, the converter:
        /// <code>
        /// Array(
        ///     (in FromContext _) => new Vector2()),
        ///     Item.Field(nameof(Vector2.X), (in Vector2 value) => ref value.X),
        ///     Item.Field(nameof(Vector2.Y), (in Vector2 value) => ref value.Y));
        /// </code>
        /// will convert a value of type 'Vector2' to an array with format:
        /// <code>[1, 2]</code>
        /// </example>
        /// </para>
        /// </summary>
        /// <remarks>
        /// An explicit converter implementation like one returned by this function should be
        /// much more performant than the default reflection-based converter.
        /// </remarks>
        public static Converter<T> Array<T>(Instantiate<T> instantiate = null, params Item<T>[] items)
        {
            var map = new Item<T>[items.Length == 0 ? 0 : items.Max(item => item.Index + 1)];
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                map[item.Index] = item;
            }
            return Create(
                (in T instance, in ToContext context) =>
                {
                    var children = new Node[map.Length];
                    for (int i = 0; i < map.Length; i++)
                        children[i] = map[i]?.Convert(instance, context) ?? Node.Null;
                    return Node.Array(children.ToArray());
                },
                instantiate,
                (ref T instance, in FromContext context) =>
                {
                    var children = context.Node.Children;
                    for (int i = 0; i < children.Length && i < map.Length; i++)
                        map[i]?.Initialize(ref instance, context.With(children[i]));
                }
            );
        }

        static Option<IConverter> CreateAttribute(Type type) => type.Members(false, true)
            .Where(member => member.IsDefined(typeof(ConverterAttribute), true))
            .Select(member => Option.Try(member, state => state switch
            {
                TypeInfo inner => Activator.CreateInstance(inner),
                FieldInfo field => field.GetValue(null),
                PropertyInfo property => property.GetValue(null),
                MethodInfo method => method.Invoke(null, System.Array.Empty<object>()),
                _ => null
            }))
            .Choose()
            .OfType<IConverter>()
            .Where(current => type.Is(current.Type, true, true))
            .FirstOrNone();

        static IConverter GetDefault(Type type) => _defaults.GetOrAdd(type, key => CreateDefault(key));

        static IConverter CreateDefault(Type type)
        {
            if (type.IsArray) return CreateArray(type);
            if (type == typeof(DateTime)) return new ConcreteDateTime();
            if (type == typeof(TimeSpan)) return new ConcreteTimeSpan();
            if (type == typeof(Guid)) return new ConcreteGuid();
            if (type == typeof(Node)) return new ConcreteNode();
            if (type.Is<Type>()) return new ConcreteType();
            if (type.GenericDefinition().TryValue(out var definition))
            {
                var arguments = type.GetGenericArguments();
                if (definition == typeof(Nullable<>)) return CreateNullable(type, arguments[0]);
                if (definition == typeof(Option<>)) return CreateOption(type, arguments[0]);
                if (definition == typeof(List<>)) return CreateList(type, arguments[0]);
                if (definition == typeof(Dictionary<,>)) return CreateDictionary(type, arguments[0], arguments[1]);
            }
            if (type.Is<IList>()) return CreateIList(type);
            if (type.Is<IDictionary>()) return CreateIDictionary(type);
            if (type.Is<IEnumerable>()) return CreateIEnumerable(type);
            if (type.Is<ISerializable>()) return CreateISerializable(type);
            return new DefaultObject(type);
        }

        static IConverter CreateOption(Type type, Type argument)
        {
            // This may fail for targets that do not support JIT compilation.
            if (Option.Try(argument, state => Activator.CreateInstance(typeof(ConcreteOption<>).MakeGenericType(state)))
                .Cast<IConverter>()
                .TryValue(out var converter))
                return converter;
            if (type.Constructors(true, false).TryFirst(current =>
                current.GetParameters().Length == 2, out var constructor))
                return new AbstractOption(constructor, argument);

            return CreateDefault(type);
        }

        static IConverter CreateNullable(Type type, Type argument) =>
            // This may fail for targets that do not support JIT compilation.
            Option.Try(argument, state => Activator.CreateInstance(typeof(ConcreteNullable<>).MakeGenericType(state)))
                .Cast<IConverter>()
                .Or(() => new AbstractNullable(type, argument));

        static IConverter CreateArray(Type type)
        {
            var element = type.GetElementType();
            switch (Type.GetTypeCode(element))
            {
                case TypeCode.Char: return new PrimitiveArray<char>(_ => _, node => node.AsChar());
                case TypeCode.Byte: return new PrimitiveArray<byte>(_ => _, node => node.AsByte());
                case TypeCode.SByte: return new PrimitiveArray<sbyte>(_ => _, node => node.AsSByte());
                case TypeCode.Int16: return new PrimitiveArray<short>(_ => _, node => node.AsShort());
                case TypeCode.Int32: return new PrimitiveArray<int>(_ => _, node => node.AsInt());
                case TypeCode.Int64: return new PrimitiveArray<long>(_ => _, node => node.AsLong());
                case TypeCode.UInt16: return new PrimitiveArray<ushort>(_ => _, node => node.AsUShort());
                case TypeCode.UInt32: return new PrimitiveArray<uint>(_ => _, node => node.AsUInt());
                case TypeCode.UInt64: return new PrimitiveArray<ulong>(_ => _, node => node.AsULong());
                case TypeCode.Single: return new PrimitiveArray<float>(_ => _, node => node.AsFloat());
                case TypeCode.Double: return new PrimitiveArray<double>(_ => _, node => node.AsDouble());
                case TypeCode.Decimal: return new PrimitiveArray<decimal>(_ => _, node => node.AsDecimal());
                case TypeCode.Boolean: return new PrimitiveArray<bool>(_ => _, node => node.AsBool());
                case TypeCode.String: return new PrimitiveArray<string>(_ => _, node => node.AsString());
                default:
                    // This may fail for targets that do not support JIT compilation.
                    return Option.Try(() => Activator.CreateInstance(typeof(ConcreteArray<>).MakeGenericType(element)))
                        .Cast<IConverter>()
                        .Or(() => new AbstractArray(element));
            }
        }

        static IConverter CreateList(Type type, Type argument)
        {
            switch (Type.GetTypeCode(argument))
            {
                case TypeCode.Char: return new PrimitiveList<char>(_ => _, node => node.AsChar());
                case TypeCode.Byte: return new PrimitiveList<byte>(_ => _, node => node.AsByte());
                case TypeCode.SByte: return new PrimitiveList<sbyte>(_ => _, node => node.AsSByte());
                case TypeCode.Int16: return new PrimitiveList<short>(_ => _, node => node.AsShort());
                case TypeCode.Int32: return new PrimitiveList<int>(_ => _, node => node.AsInt());
                case TypeCode.Int64: return new PrimitiveList<long>(_ => _, node => node.AsLong());
                case TypeCode.UInt16: return new PrimitiveList<ushort>(_ => _, node => node.AsUShort());
                case TypeCode.UInt32: return new PrimitiveList<uint>(_ => _, node => node.AsUInt());
                case TypeCode.UInt64: return new PrimitiveList<ulong>(_ => _, node => node.AsULong());
                case TypeCode.Single: return new PrimitiveList<float>(_ => _, node => node.AsFloat());
                case TypeCode.Double: return new PrimitiveList<double>(_ => _, node => node.AsDouble());
                case TypeCode.Decimal: return new PrimitiveList<decimal>(_ => _, node => node.AsDecimal());
                case TypeCode.Boolean: return new PrimitiveList<bool>(_ => _, node => node.AsBool());
                case TypeCode.String: return new PrimitiveList<string>(_ => _, node => node.AsString());
                default:
                    // This may fail for targets that do not support JIT compilation.
                    return Option.Try(() => Activator.CreateInstance(typeof(ConcreteList<>).MakeGenericType(argument)))
                        .Cast<IConverter>()
                        .Or(() => CreateIList(type));
            }
        }

        static IConverter CreateIList(Type type) => CreateIEnumerable(type);

        static IConverter CreateIEnumerable(Type type)
        {
            if (type.EnumerableArgument(true).TryValue(out var argument) &&
                type.EnumerableConstructor(true).TryValue(out var constructor))
            {
                switch (Type.GetTypeCode(argument))
                {
                    case TypeCode.Char: return new PrimitiveEnumerable<char>(_ => _, node => node.AsChar(), constructor);
                    case TypeCode.Byte: return new PrimitiveEnumerable<byte>(_ => _, node => node.AsByte(), constructor);
                    case TypeCode.SByte: return new PrimitiveEnumerable<sbyte>(_ => _, node => node.AsSByte(), constructor);
                    case TypeCode.Int16: return new PrimitiveEnumerable<short>(_ => _, node => node.AsShort(), constructor);
                    case TypeCode.Int32: return new PrimitiveEnumerable<int>(_ => _, node => node.AsInt(), constructor);
                    case TypeCode.Int64: return new PrimitiveEnumerable<long>(_ => _, node => node.AsLong(), constructor);
                    case TypeCode.UInt16: return new PrimitiveEnumerable<ushort>(_ => _, node => node.AsUShort(), constructor);
                    case TypeCode.UInt32: return new PrimitiveEnumerable<uint>(_ => _, node => node.AsUInt(), constructor);
                    case TypeCode.UInt64: return new PrimitiveEnumerable<ulong>(_ => _, node => node.AsULong(), constructor);
                    case TypeCode.Single: return new PrimitiveEnumerable<float>(_ => _, node => node.AsFloat(), constructor);
                    case TypeCode.Double: return new PrimitiveEnumerable<double>(_ => _, node => node.AsDouble(), constructor);
                    case TypeCode.Decimal: return new PrimitiveEnumerable<decimal>(_ => _, node => node.AsDecimal(), constructor);
                    case TypeCode.Boolean: return new PrimitiveEnumerable<bool>(_ => _, node => node.AsBool(), constructor);
                    case TypeCode.String: return new PrimitiveEnumerable<string>(_ => _, node => node.AsString(), constructor);
                    default:
                        // This may fail for targets that do not support JIT compilation.
                        return Option.Try(() => Activator.CreateInstance(typeof(AbstractEnumerable<>).MakeGenericType(argument), constructor))
                            .Cast<IConverter>()
                            .Or(() => new AbstractEnumerable(argument, constructor));
                }
            }

            return Option.And(type.EnumerableArgument(false), type.EnumerableConstructor(false))
                .Map(pair => new AbstractEnumerable(pair.Item1, pair.Item2))
                .Cast<IConverter>()
                .Or(() => CreateDefault(type));
        }

        static IConverter CreateDictionary(Type type, Type key, Type value) =>
            // This may fail for targets that do not support JIT compilation.
            Option.Try(() => Activator.CreateInstance(typeof(ConcreteDictionary<,>).MakeGenericType(key, value)))
                .Cast<IConverter>()
                .Or(() => CreateIDictionary(type));

        static IConverter CreateIDictionary(Type type)
        {
            if (type.DefaultConstructor().TryValue(out var constructor))
            {
                if (type.DictionaryArguments(true).TryValue(out var types))
                {
                    // This may fail for targets that do not support JIT compilation.
                    return Option.Try(() => Activator.CreateInstance(typeof(AbstractDictionary<,>).MakeGenericType(types.key, types.value)))
                        .Cast<IConverter>()
                        .Or(() => new AbstractDictionary(types.key, types.value, constructor));
                }

                if (type.DictionaryArguments(false).TryValue(out types))
                    return new AbstractDictionary(types.key, types.value, constructor);
            }

            return CreateIEnumerable(type);
        }

        static IConverter CreateISerializable(Type type) => type.SerializableConstructor()
            .Map(constructor => new AbstractSerializable(constructor))
            .Cast<IConverter>()
            .Or(() => CreateDefault(type));
    }
}