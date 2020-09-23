using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Entia.Json.Converters
{
    public sealed class PrimitiveEnumerable<T> : Converter<IEnumerable<T>>
    {
        readonly Func<T, Node> _to;
        readonly Func<Node, T> _from;
        readonly ConstructorInfo _constructor;

        public PrimitiveEnumerable(Func<T, Node> to, Func<Node, T> from, ConstructorInfo constructor)
        {
            _to = to;
            _from = from;
            _constructor = constructor;
        }

        public override Node Convert(in IEnumerable<T> instance, in ToContext context)
        {
            if (instance is IList<T> list)
            {
                var items = new Node[list.Count];
                for (int i = 0; i < items.Length; i++) items[i] = _to(list[i]);
                return Node.Array(items);
            }
            else if (instance is ICollection<T> collection)
            {
                var items = new Node[collection.Count];
                var index = 0;
                foreach (var value in collection) items[index++] = _to(value);
                return Node.Array(items);
            }
            else
            {
                var items = new List<Node>();
                foreach (var value in instance) items.Add(_to(value));
                return Node.Array(items.ToArray());
            }
        }

        public override IEnumerable<T> Instantiate(in FromContext context) =>
            FormatterServices.GetUninitializedObject(context.Type) as IEnumerable<T>;

        public override void Initialize(ref IEnumerable<T> instance, in FromContext context)
        {
            var children = context.Node.Children;
            var items = new T[children.Length];
            for (int i = 0; i < children.Length; i++) items[i] = _from(children[i]);
            _constructor.Invoke(instance, new object[] { items });
        }
    }

    public sealed class AbstractEnumerable<T> : Converter<IEnumerable<T>>
    {
        static readonly IConverter _converter = Converter.Default<T>();

        readonly ConstructorInfo _constructor;

        public AbstractEnumerable(ConstructorInfo constructor) { _constructor = constructor; }

        public override Node Convert(in IEnumerable<T> instance, in ToContext context)
        {
            if (instance is IList<T> list)
            {
                var items = new Node[list.Count];
                for (int i = 0; i < items.Length; i++) items[i] = context.Convert(list[i], _converter);
                return Node.Array(items);
            }
            else if (instance is ICollection<T> collection)
            {
                var items = new Node[collection.Count];
                var index = 0;
                foreach (var value in collection) items[index++] = context.Convert(value, _converter);
                return Node.Array(items);
            }
            else
            {
                var items = new List<Node>();
                foreach (var value in instance) items.Add(context.Convert(value, _converter));
                return Node.Array(items.ToArray());
            }
        }

        public override IEnumerable<T> Instantiate(in FromContext context) =>
            FormatterServices.GetUninitializedObject(context.Type) as IEnumerable<T>;

        public override void Initialize(ref IEnumerable<T> instance, in FromContext context)
        {
            var children = context.Node.Children;
            var items = new T[children.Length];
            for (int i = 0; i < children.Length; i++) items[i] = context.Convert<T>(children[i], _converter);
            _constructor.Invoke(instance, new object[] { items });
        }
    }

    public sealed class AbstractEnumerable : Converter<IEnumerable>
    {
        readonly Type _element;
        readonly ConstructorInfo _constructor;
        readonly IConverter _converter;

        public AbstractEnumerable(Type element, ConstructorInfo constructor)
        {
            _element = element;
            _constructor = constructor;
            _converter = Converter.Default(element);
        }

        public override Node Convert(in IEnumerable instance, in ToContext context)
        {
            if (instance is IList list)
            {
                var items = new Node[list.Count];
                for (int i = 0; i < items.Length; i++) items[i] = context.Convert(list[i], _element, _converter);
                return Node.Array(items);
            }
            else if (instance is ICollection collection)
            {
                var items = new Node[collection.Count];
                var index = 0;
                foreach (var value in collection) items[index++] = context.Convert(value, _element, _converter);
                return Node.Array(items);
            }
            else
            {
                var items = new List<Node>(8);
                foreach (var value in instance) items.Add(context.Convert(value, _element, _converter));
                return Node.Array(items.ToArray());
            }
        }

        public override IEnumerable Instantiate(in FromContext context) =>
            FormatterServices.GetUninitializedObject(context.Type) as IEnumerable;

        public override void Initialize(ref IEnumerable instance, in FromContext context)
        {
            var children = context.Node.Children;
            var items = Array.CreateInstance(_element, children.Length);
            for (int i = 0; i < children.Length; i++)
                items.SetValue(context.Convert(children[i], _element, _converter), i);
            _constructor.Invoke(instance, new object[] { items });
        }
    }
}