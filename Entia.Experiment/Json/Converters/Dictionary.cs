using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Entia.Core;
using Entia.Core.Providers;

namespace Entia.Json.Converters
{
    namespace Providers
    {
        public sealed class Dictionary : Provider<IConverter>
        {
            public override IEnumerable<IConverter> Provide(Type type)
            {
                var arguments = type.GetGenericArguments();
                if (type.Is(typeof(Dictionary<,>), definition: true) &&
                    Option.Try(arguments, state => Activator.CreateInstance(typeof(ConcreteDictionary<,>).MakeGenericType(state)))
                    .Cast<IConverter>()
                    .TryValue(out var converter))
                    yield return converter;
                if (Option.Try(arguments, state => Activator.CreateInstance(typeof(AbstractDictionary<,>).MakeGenericType(state)))
                    .Cast<IConverter>()
                    .TryValue(out converter))
                    yield return converter;
                yield return new AbstractDictionary(arguments[0], arguments[1]);
            }
        }
    }

    public sealed class ConcreteDictionary<TKey, TValue> : Converter<Dictionary<TKey, TValue>>
    {
        public override Node Convert(in Dictionary<TKey, TValue> instance, in ConvertToContext context)
        {
            var members = new Node[instance.Count];
            var index = 0;
            foreach (var pair in instance)
                members[index++] = Node.Array(context.Convert(pair.Key), context.Convert(pair.Value));
            return Node.Array(members);
        }

        public override Dictionary<TKey, TValue> Instantiate(in ConvertFromContext context) =>
            new Dictionary<TKey, TValue>(context.Node.Children.Length);

        public override void Initialize(ref Dictionary<TKey, TValue> instance, in ConvertFromContext context)
        {
            if (context.Node.IsArray())
            {
                foreach (var child in context.Node.Children)
                {
                    if (child.TryItem(0, out var key) && child.TryItem(1, out var value))
                        instance.Add(context.Convert<TKey>(key), context.Convert<TValue>(value));
                }
            }
            else if (context.Node.IsObject())
            {
                foreach (var child in context.Node.Children)
                {
                    if (child.TryMember(out var key, out var value))
                        instance.Add(context.Convert<TKey>(key), context.Convert<TValue>(value));
                }
            }
        }
    }

    public sealed class AbstractDictionary<TKey, TValue> : Converter<IDictionary<TKey, TValue>>
    {
        public override bool CanConvert(TypeData type) => type.DefaultConstructor is ConstructorInfo;

        public override Node Convert(in IDictionary<TKey, TValue> instance, in ConvertToContext context)
        {
            var members = new Node[instance.Count];
            var index = 0;
            foreach (var pair in instance)
                members[index++] = Node.Array(context.Convert(pair.Key), context.Convert(pair.Value));
            return Node.Array(members);
        }

        public override IDictionary<TKey, TValue> Instantiate(in ConvertFromContext context) =>
            context.Type.DefaultConstructor.Invoke(Array.Empty<object>()) as IDictionary<TKey, TValue>;

        public override void Initialize(ref IDictionary<TKey, TValue> instance, in ConvertFromContext context)
        {
            if (context.Node.IsArray())
            {
                foreach (var child in context.Node.Children)
                {
                    if (child.TryItem(0, out var key) && child.TryItem(1, out var value))
                        instance.Add(context.Convert<TKey>(key), context.Convert<TValue>(value));
                }
            }
            else if (context.Node.IsObject())
            {
                foreach (var child in context.Node.Children)
                {
                    if (child.TryMember(out var key, out var value))
                        instance.Add(context.Convert<TKey>(key), context.Convert<TValue>(value));
                }
            }
        }
    }

    public sealed class AbstractDictionary : Converter<IDictionary>
    {
        readonly Type _key;
        readonly Type _value;

        public AbstractDictionary() : this(typeof(object), typeof(object)) { }
        public AbstractDictionary(Type key, Type value)
        {
            _key = key;
            _value = value;
        }

        public override bool CanConvert(TypeData type) => type.DefaultConstructor is ConstructorInfo;

        public override Node Convert(in IDictionary instance, in ConvertToContext context)
        {
            var members = new Node[instance.Count];
            var index = 0;
            foreach (var key in instance.Keys)
                members[index++] = Node.Array(context.Convert(key, _key), context.Convert(instance[key], _value));
            return Node.Array(members);
        }

        public override IDictionary Instantiate(in ConvertFromContext context) =>
            context.Type.DefaultConstructor.Invoke(Array.Empty<object>()) as IDictionary;

        public override void Initialize(ref IDictionary instance, in ConvertFromContext context)
        {
            if (context.Node.IsArray())
            {
                foreach (var child in context.Node.Children)
                {
                    if (child.TryItem(0, out var key) && child.TryItem(1, out var value))
                        instance.Add(context.Convert(key, _key), context.Convert(value, _value));
                }
            }
            else if (context.Node.IsObject())
            {
                foreach (var child in context.Node.Children)
                {
                    if (child.TryMember(out var key, out var value))
                        instance.Add(context.Convert(key, _key), context.Convert(value, _value));
                }
            }
        }
    }
}