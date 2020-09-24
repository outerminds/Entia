using System;
using System.Collections.Generic;

namespace Entia.Json.Converters
{
    public sealed class PrimitiveList<T> : Converter<List<T>>
    {
        readonly Func<T, Node> _to;
        readonly Func<Node, T> _from;

        public PrimitiveList(Func<T, Node> to, Func<Node, T> from)
        {
            _to = to;
            _from = from;
        }

        public override Node Convert(in List<T> instance, in ToContext context)
        {
            var items = new Node[instance.Count];
            for (int i = 0; i < items.Length; i++) items[i] = _to(instance[i]);
            return Node.Array(items);
        }

        public override List<T> Instantiate(in FromContext context) => new List<T>(context.Node.Children.Length);

        public override void Initialize(ref List<T> instance, in FromContext context)
        {
            var children = context.Node.Children;
            for (int i = 0; i < children.Length; i++) instance.Add(_from(children[i]));
        }
    }

    public sealed class ConcreteList<T> : Converter<List<T>>
    {
        static readonly IConverter _converter = Converter.Default<T>();

        public override Node Convert(in List<T> instance, in ToContext context)
        {
            var items = new Node[instance.Count];
            for (int i = 0; i < items.Length; i++) items[i] = context.Convert(instance[i], _converter);
            return Node.Array(items);
        }

        public override List<T> Instantiate(in FromContext context) => new List<T>(context.Node.Children.Length);

        public override void Initialize(ref List<T> instance, in FromContext context)
        {
            var children = context.Node.Children;
            for (int i = 0; i < children.Length; i++) instance.Add(context.Convert<T>(children[i], _converter));
        }
    }
}