using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Entia.Core;

namespace Entia.Json.Converters
{
    [Implementation(typeof(Node), typeof(ConcreteNode))]
    [Implementation(typeof(Type), typeof(AbstractType))]
    [Implementation(typeof(DateTime), typeof(ConcreteDateTime))]
    [Implementation(typeof(TimeSpan), typeof(ConcreteTimeSpan))]
    [Implementation(typeof(Guid), typeof(ConcreteGuid))]
    [Implementation(typeof(Nullable<>), typeof(Providers.Nullable))]
    [Implementation(typeof(Array), typeof(Providers.Array))]
    [Implementation(typeof(List<>), typeof(Providers.List))]
    [Implementation(typeof(IList), typeof(AbstractList))]
    [Implementation(typeof(IDictionary<,>), typeof(Providers.Dictionary))]
    [Implementation(typeof(IDictionary), typeof(AbstractDictionary))]
    [Implementation(typeof(IEnumerable), typeof(Providers.Enumerable))]
    [Implementation(typeof(ISerializable), typeof(AbstractSerializable))]
    public interface IConverter : ITrait
    {
        bool Validate(TypeData type);
        Node Convert(in ConvertToContext context);
        object Instantiate(in ConvertFromContext context);
        void Initialize(ref object instance, in ConvertFromContext context);
    }

    public abstract class Converter<T> : IConverter
    {
        public virtual bool Validate(TypeData type) => true;
        public abstract Node Convert(in T instance, in ConvertToContext context);
        public abstract T Instantiate(in ConvertFromContext context);
        public virtual void Initialize(ref T instance, in ConvertFromContext context) { }

        Node IConverter.Convert(in ConvertToContext context) =>
            context.Instance is T casted ? Convert(casted, context) : Node.Null;
        object IConverter.Instantiate(in ConvertFromContext context) => Instantiate(context);
        void IConverter.Initialize(ref object instance, in ConvertFromContext context)
        {
            if (instance is T casted)
            {
                Initialize(ref casted, context);
                instance = casted;
            }
        }
    }

    public static class Converter
    {
        public delegate Option<(int version, Node node)> Upgrade(Node node);
        public delegate bool Validate(TypeData type);
        public delegate Node Convert<T>(in T instance, in ConvertToContext context);
        public delegate T Instantiate<T>(in ConvertFromContext context);
        public delegate void Initialize<T>(ref T instance, in ConvertFromContext context);

        sealed class Function<T> : Converter<T>
        {
            public readonly Convert<T> _convert;
            public readonly Instantiate<T> _instantiate;
            public readonly Initialize<T> _initialize;
            public readonly Validate _validate;

            public Function(Convert<T> convert, Instantiate<T> instantiate, Initialize<T> initialize, Validate validate)
            {
                _convert = convert;
                _instantiate = instantiate;
                _initialize = initialize;
                _validate = validate;
            }

            public override bool Validate(TypeData type) => _validate(type);
            public override Node Convert(in T instance, in ConvertToContext context) => _convert(instance, context);
            public override T Instantiate(in ConvertFromContext context) => _instantiate(context);
            public override void Initialize(ref T instance, in ConvertFromContext context) => _initialize(ref instance, context);
        }

        static class Cache<T>
        {
            public static readonly Instantiate<T> Instantiate = (in ConvertFromContext context) => context.Instantiate<T>();
            public static readonly Initialize<T> Initialize = (ref T _, in ConvertFromContext __) => { };
        }

        public static Converter<T> Version<T>(params (int version, Converter<T> converter)[] converters) =>
            Version(converters.Min(pair => pair.version), converters.Max(pair => pair.version), converters);
        public static Converter<T> Version<T>(int @default, int latest, params (int version, Converter<T> converter)[] converters)
        {
            if (converters.Length == 0) return Default<T>();
            var versionToConverter = converters.ToDictionary(pair => pair.version, pair => pair.converter);
            var defaultConverter = versionToConverter[@default];
            var latestConverter = versionToConverter[latest];

            Converter<T> Converter(Node node, out Node value)
            {
                var pair =
                    node.IsObject() && node.Children.Length == 4 &&
                    node.Children[0].AsString() == "$k" && node.Children[2].AsString() == "$v" ?
                    (version: node.Children[1].AsInt(), value: node.Children[3]) : (version: @default, value: node);
                value = pair.value;
                return versionToConverter.TryGetValue(pair.version, out var converter) ? converter : defaultConverter;
            }

            return Create(
                (in T instance, in ConvertToContext context) =>
                    Node.Object("$k", latest, "$v", latestConverter.Convert(instance, context)),
                (in ConvertFromContext context) =>
                    Converter(context.Node, out var value).Instantiate(context.With(value)),
                (ref T instance, in ConvertFromContext context) =>
                    Converter(context.Node, out var value).Initialize(ref instance, context.With(value)));
        }

        public static Converter<T> Default<T>() => Create(
            (in T _, in ConvertToContext __) => Node.Null,
            (in ConvertFromContext _) => default,
            (ref T _, in ConvertFromContext __) => { },
            _ => false);

        public static Converter<T> Object<T>(Instantiate<T> instantiate = null, params Member<T>[] members)
        {
            var map = members
                .Select(member => member.Aliases.Prepend(member.Name).Select(name => (member, name)))
                .Flatten()
                .ToDictionary(pair => pair.name, pair => pair.member);
            return Create(
                (in T instance, in ConvertToContext context) =>
                {
                    var nodes = new List<Node>(members.Length * 2);
                    for (int i = 0; i < members.Length; i++)
                    {
                        var member = members[i];
                        if (member.Convert(instance, context) is Node node)
                        {
                            nodes.Add(member.Name);
                            nodes.Add(node);
                        }
                    }
                    return Node.Object(nodes.ToArray());
                },
                instantiate,
                (ref T instance, in ConvertFromContext context) =>
                {
                    foreach (var (key, value) in context.Node.Members())
                    {
                        if (map.TryGetValue(key, out var member))
                            member.Initialize(ref instance, context.With(value));
                    }
                }
            );
        }

        public static Converter<TSource> Create<TSource, TTarget>(InFunc<TSource, TTarget> to, InFunc<TTarget, TSource> from) => Create(
            (in TSource instance, in ConvertToContext context) => context.Convert(to(instance)),
            (in ConvertFromContext context) => from(context.Convert<TTarget>(context.Node)));

        public static Converter<T> Create<T>(InFunc<T, Node> to, Func<Node, T> from) => Create(
            (in T instance, in ConvertToContext context) => to(instance),
            (in ConvertFromContext context) => from(context.Node));

        public static Converter<T> Create<T>(Convert<T> convert, Instantiate<T> instantiate = null, Initialize<T> initialize = null, Validate validate = null) =>
            new Function<T>(convert, instantiate ?? Cache<T>.Instantiate, initialize ?? Cache<T>.Initialize, validate ?? (_ => true));
    }
}