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
            public override IEnumerable<IConverter> Provide(TypeData type)
            {
                {
                    if (type.Interfaces.TryFirst(@interface => @interface.Definition == typeof(IDictionary<,>), out var @interface) &&
                        @interface.Arguments.Length == 2)
                    {
                        var arguments = @interface.Arguments.Select(argument => argument.Type);
                        if (type.Definition == typeof(Dictionary<,>) &&
                            Core.Option.Try(() => Activator.CreateInstance(typeof(ConcreteDictionary<,>).MakeGenericType(arguments)))
                            .Cast<IConverter>()
                            .TryValue(out var converter))
                            yield return converter;
                        if (type.DefaultConstructor.TryValue(out var constructor))
                        {
                            if (Core.Option.Try(() => Activator.CreateInstance(typeof(AbstractDictionary<,>).MakeGenericType(arguments), constructor))
                                .Cast<IConverter>()
                                .TryValue(out converter))
                                yield return converter;
                            yield return new AbstractDictionary(arguments[0], arguments[1], constructor);
                        }
                    };
                }
                {
                    if (type.Interfaces.Any(@interface => @interface == typeof(IDictionary)) &&
                        type.DefaultConstructor.TryValue(out var constructor))
                        yield return new AbstractDictionary(typeof(object), typeof(object), constructor);
                }
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
        readonly ConstructorData _constructor;

        public AbstractDictionary(ConstructorData constructor) { _constructor = constructor; }

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
            _constructor.Constructor.Invoke(Array.Empty<object>()) as IDictionary<TKey, TValue>;

        public override void Initialize(ref IDictionary<TKey, TValue> instance, in ConvertFromContext context)
        {
            var children = context.Node.Children;
            for (int i = 1; i < children.Length; i += 2)
                instance.Add(context.Convert<TKey>(children[i - 1]), context.Convert<TValue>(children[i]));
        }
    }

    public sealed class AbstractDictionary : Converter<IDictionary>
    {
        readonly TypeData _key;
        readonly TypeData _value;
        readonly ConstructorData _constructor;

        public AbstractDictionary(TypeData key, TypeData value, ConstructorData constructor)
        {
            _key = key;
            _value = value;
            _constructor = constructor;
        }

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
            _constructor.Constructor.Invoke(Array.Empty<object>()) as IDictionary;

        public override void Initialize(ref IDictionary instance, in ConvertFromContext context)
        {
            var children = context.Node.Children;
            for (int i = 1; i < children.Length; i += 2)
                instance.Add(context.Convert(children[i - 1], _key), context.Convert(children[i], _value));
        }
    }
}