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
        bool CanConvert(TypeData type);
        Node Convert(in ConvertToContext context);
        object Instantiate(in ConvertFromContext context);
        void Initialize(ref object instance, in ConvertFromContext context);
    }

    public abstract class Converter<T> : IConverter
    {
        public virtual bool CanConvert(TypeData type) => true;
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
        public delegate Node Convert<T>(in T instance, in ConvertToContext context);
        public delegate T Instantiate<T>(in ConvertFromContext context);
        public delegate void Initialize<T>(ref T instance, in ConvertFromContext context);

        sealed class Function<T> : Converter<T>
        {
            public readonly Convert<T> _convert;
            public readonly Instantiate<T> _instantiate;
            public readonly Initialize<T> _initialize;

            public Function(Convert<T> convert, Instantiate<T> instantiate, Initialize<T> initialize)
            {
                _convert = convert;
                _instantiate = instantiate;
                _initialize = initialize;
            }

            public override Node Convert(in T instance, in ConvertToContext context) => _convert(instance, context);
            public override T Instantiate(in ConvertFromContext context) => _instantiate(context);
            public override void Initialize(ref T instance, in ConvertFromContext context) => _initialize(ref instance, context);
        }

        static class Cache<T>
        {
            public static readonly Instantiate<T> Instantiate = (in ConvertFromContext context) => context.Instantiate<T>();
            public static readonly Initialize<T> Initialize = (ref T _, in ConvertFromContext __) => { };
        }

        public static Converter<T> Object<T>(Instantiate<T> instantiate, params IMember<T>[] members)
        {
            var map = members.ToDictionary(member => member.Name);
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
                            member.Initialize(ref instance, value, context);
                    }
                }
            );
        }

        public static Converter<T> Object<T>(params IMember<T>[] members) => Object(null, members);

        public static Converter<TSource> Create<TSource, TTarget>(InFunc<TSource, TTarget> to, InFunc<TTarget, TSource> from) => Create(
            (in TSource instance, in ConvertToContext context) => context.Convert(to(instance)),
            (in ConvertFromContext context) => from(context.Convert<TTarget>(context.Node)));

        public static Converter<T> Create<T>(InFunc<T, Node> to, Func<Node, T> from) => Create(
            (in T instance, in ConvertToContext context) => to(instance),
            (in ConvertFromContext context) => from(context.Node));

        public static Converter<T> Create<T>(Convert<T> convert, Instantiate<T> instantiate = null, Initialize<T> initialize = null) =>
            new Function<T>(convert, instantiate ?? Cache<T>.Instantiate, initialize ?? Cache<T>.Initialize);
    }

    public interface IMember<T>
    {
        string Name { get; }

        Node Convert(in T instance, in ConvertToContext context);
        void Initialize(ref T instance, Node node, in ConvertFromContext context);
    }

    public static class Member
    {
        public delegate bool Validate<T>(in T instance);
        public delegate Node Convert<T>(in T instance, in ConvertToContext context);
        public delegate void Initialize<T>(ref T instance, Node node, in ConvertFromContext context);
        public delegate ref readonly TValue Get<T, TValue>(in T instance);
        public delegate TValue Getter<T, TValue>(in T instance);
        public delegate void Setter<T, TValue>(ref T instance, in TValue value);
        public delegate Node To<T>(in T instance, in ConvertToContext context);
        public delegate T From<T>(Node node, in ConvertFromContext context);

        sealed class Function<T> : IMember<T>
        {
            public string Name { get; }

            readonly Convert<T> _convert;
            readonly Initialize<T> _initialize;

            public Function(string name, Convert<T> convert, Initialize<T> initialize)
            {
                Name = name;
                _convert = convert;
                _initialize = initialize;
            }

            public Node Convert(in T instance, in ConvertToContext context) => _convert(instance, context);
            public void Initialize(ref T instance, Node node, in ConvertFromContext context) => _initialize(ref instance, node, context);
        }

        static class Cache<T>
        {
            public static readonly To<T> To = (in T value, in ConvertToContext context) => context.Convert(value);
            public static readonly From<T> From = (Node node, in ConvertFromContext context) => context.Convert<T>(node);
            public static readonly Validate<T> Validate = (in T _) => true;
        }

        public static IMember<T> Field<T, TValue>(string name, Get<T, TValue> get, To<TValue> to = null, From<TValue> from = null, Validate<TValue> validate = null)
        {
            to ??= Cache<TValue>.To;
            from ??= Cache<TValue>.From;
            validate ??= Cache<TValue>.Validate;
            return Create(name,
                (in T instance, in ConvertToContext context) =>
                {
                    ref readonly var value = ref get(instance);
                    if (validate(value)) return to(value, context);
                    return default;
                },
                (ref T instance, Node node, in ConvertFromContext context) =>
                {
                    var value = from(node, context);
                    if (validate(value)) UnsafeUtility.Set(get(instance), value);
                });
        }

        public static IMember<T> Property<T, TValue>(string name, Getter<T, TValue> get, Setter<T, TValue> set, To<TValue> to = null, From<TValue> from = null, Validate<TValue> validate = null)
        {
            to ??= Cache<TValue>.To;
            from ??= Cache<TValue>.From;
            validate ??= Cache<TValue>.Validate;
            return Create(name,
                (in T instance, in ConvertToContext context) =>
                {
                    var value = get(instance);
                    if (validate(value)) return to(value, context);
                    return default;
                },
                (ref T instance, Node node, in ConvertFromContext context) =>
                {
                    var value = from(node, context);
                    if (validate(value)) set(ref instance, value);
                });
        }

        public static IMember<T> Create<T>(string name, Convert<T> convert, Initialize<T> initialize) =>
            new Function<T>(name, convert, initialize);
    }
}