using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Json.Converters
{
    public sealed class ConcreteDictionary<TKey, TValue> : Converter<Dictionary<TKey, TValue>>
    {
        static readonly (IConverter key, IConverter value) _converters =
            (Converter.Default<TKey>(), Converter.Default<TValue>());

        public override Node Convert(in Dictionary<TKey, TValue> instance, in ToContext context)
        {
            var items = new Node[instance.Count * 2];
            var index = 0;
            foreach (var pair in instance)
            {
                items[index++] = context.Convert(pair.Key, _converters.key);
                items[index++] = context.Convert(pair.Value, _converters.value);
            }
            return typeof(TKey) == typeof(string) ? Node.Object(items) : Node.Array(items);
        }

        public override Dictionary<TKey, TValue> Instantiate(in FromContext context) =>
            new Dictionary<TKey, TValue>(context.Node.Children.Length);

        public override void Initialize(ref Dictionary<TKey, TValue> instance, in FromContext context)
        {
            var children = context.Node.Children;
            for (int i = 1; i < children.Length; i += 2) instance.Add(
                context.Convert<TKey>(children[i - 1], _converters.key),
                context.Convert<TValue>(children[i], _converters.value));
        }
    }

    public sealed class AbstractDictionary<TKey, TValue> : Converter<IDictionary<TKey, TValue>>
    {
        static readonly (IConverter key, IConverter value) _converters =
            (Converter.Default<TKey>(), Converter.Default<TValue>());

        readonly ConstructorInfo _constructor;

        public AbstractDictionary(ConstructorInfo constructor) { _constructor = constructor; }

        public override Node Convert(in IDictionary<TKey, TValue> instance, in ToContext context)
        {
            var items = new Node[instance.Count * 2];
            var index = 0;
            foreach (var pair in instance)
            {
                items[index++] = context.Convert(pair.Key, _converters.key);
                items[index++] = context.Convert(pair.Value, _converters.value);
            }
            return typeof(TKey) == typeof(string) ? Node.Object(items) : Node.Array(items);
        }

        public override IDictionary<TKey, TValue> Instantiate(in FromContext context) =>
            _constructor.Invoke(Array.Empty<object>()) as IDictionary<TKey, TValue>;

        public override void Initialize(ref IDictionary<TKey, TValue> instance, in FromContext context)
        {
            var children = context.Node.Children;
            for (int i = 1; i < children.Length; i += 2) instance.Add(
                context.Convert<TKey>(children[i - 1], _converters.key),
                context.Convert<TValue>(children[i], _converters.value));
        }
    }

    public sealed class AbstractDictionary : Converter<IDictionary>
    {
        readonly Type _key;
        readonly Type _value;
        readonly ConstructorInfo _constructor;
        readonly (IConverter key, IConverter value) _converters;

        public AbstractDictionary(Type key, Type value, ConstructorInfo constructor)
        {
            _key = key;
            _value = value;
            _constructor = constructor;
            _converters = (Converter.Default(key), Converter.Default(value));
        }

        public override Node Convert(in IDictionary instance, in ToContext context)
        {
            var items = new Node[instance.Count * 2];
            var index = 0;
            foreach (var key in instance.Keys)
            {
                items[index++] = context.Convert(key, _key, _converters.key);
                items[index++] = context.Convert(instance[key], _value, _converters.value);
            }
            return _key == typeof(string) ? Node.Object(items) : Node.Array(items);
        }

        public override IDictionary Instantiate(in FromContext context) =>
            _constructor.Invoke(Array.Empty<object>()) as IDictionary;

        public override void Initialize(ref IDictionary instance, in FromContext context)
        {
            var children = context.Node.Children;
            for (int i = 1; i < children.Length; i += 2) instance.Add(
                context.Convert(children[i - 1], _key, _converters.key),
                context.Convert(children[i], _value, _converters.value));
        }
    }
}