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
            var items = new Node[instance.Count * 2];
            var index = 0;
            foreach (var pair in instance)
            {
                items[index++] = context.Convert(pair.Key);
                items[index++] = context.Convert(pair.Value);
            }
            return Node.Array(items);
        }

        public override Dictionary<TKey, TValue> Instantiate(in ConvertFromContext context) =>
            new Dictionary<TKey, TValue>(context.Node.Children.Length);

        public override void Initialize(ref Dictionary<TKey, TValue> instance, in ConvertFromContext context)
        {
            var children = context.Node.Children;
            for (int i = 1; i < children.Length; i += 2)
                instance.Add(context.Convert<TKey>(children[i - 1]), context.Convert<TValue>(children[i]));
        }
    }

    public sealed class AbstractDictionary<TKey, TValue> : Converter<IDictionary<TKey, TValue>>
    {
        public override bool CanConvert(TypeData type) => type.DefaultConstructor is ConstructorInfo;

        public override Node Convert(in IDictionary<TKey, TValue> instance, in ConvertToContext context)
        {
            var items = new Node[instance.Count * 2];
            var index = 0;
            foreach (var pair in instance)
            {
                items[index++] = context.Convert(pair.Key);
                items[index++] = context.Convert(pair.Value);
            }
            return Node.Array(items);
        }

        public override IDictionary<TKey, TValue> Instantiate(in ConvertFromContext context) =>
            context.Type.DefaultConstructor.Invoke(Array.Empty<object>()) as IDictionary<TKey, TValue>;

        public override void Initialize(ref IDictionary<TKey, TValue> instance, in ConvertFromContext context)
        {
            var children = context.Node.Children;
            for (int i = 1; i < children.Length; i += 2)
                instance.Add(context.Convert<TKey>(children[i - 1]), context.Convert<TValue>(children[i]));
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
            var items = new Node[instance.Count * 2];
            var index = 0;
            foreach (var key in instance.Keys)
            {
                items[index++] = context.Convert(key, _key);
                items[index++] = context.Convert(instance[key], _value);
            }
            return Node.Array(items);
        }

        public override IDictionary Instantiate(in ConvertFromContext context) =>
            context.Type.DefaultConstructor.Invoke(Array.Empty<object>()) as IDictionary;

        public override void Initialize(ref IDictionary instance, in ConvertFromContext context)
        {
            var children = context.Node.Children;
            for (int i = 1; i < children.Length; i += 2)
                instance.Add(context.Convert(children[i - 1], _key), context.Convert(children[i], _value));
        }
    }
}