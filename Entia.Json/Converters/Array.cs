using System;

namespace Entia.Json.Converters
{
    public sealed class PrimitiveArray<T> : Converter<T[]>
    {
        readonly Func<T, Node> _to;
        readonly Func<Node, T> _from;

        public PrimitiveArray(Func<T, Node> to, Func<Node, T> from)
        {
            _to = to;
            _from = from;
        }

        public override Node Convert(in T[] instance, in ToContext context)
        {
            var items = new Node[instance.Length];
            for (int i = 0; i < instance.Length; i++) items[i] = _to(instance[i]);
            return Node.Array(items);
        }

        public override T[] Instantiate(in FromContext context) => new T[context.Node.Children.Length];

        public override void Initialize(ref T[] instance, in FromContext context)
        {
            var children = context.Node.Children;
            for (int i = 0; i < children.Length; i++) instance[i] = _from(children[i]);
        }
    }

    public sealed class ConcreteArray<T> : Converter<T[]>
    {
        static readonly IConverter _converter = Converter.Default<T>();

        public override Node Convert(in T[] instance, in ToContext context)
        {
            var items = new Node[instance.Length];
            for (int i = 0; i < instance.Length; i++) items[i] = context.Convert(instance[i], _converter);
            return Node.Array(items);
        }

        public override T[] Instantiate(in FromContext context) => new T[context.Node.Children.Length];

        public override void Initialize(ref T[] instance, in FromContext context)
        {
            var children = context.Node.Children;
            for (int i = 0; i < children.Length; i++) instance[i] = context.Convert<T>(children[i], _converter);
        }
    }

    public sealed class AbstractArray : Converter<Array>
    {
        readonly Type _element;
        readonly IConverter _converter;

        public AbstractArray(Type element)
        {
            _element = element;
            _converter = Converter.Default(element);
        }

        public override Node Convert(in Array instance, in ToContext context)
        {
            var items = new Node[instance.Length];
            for (int i = 0; i < instance.Length; i++) items[i] = context.Convert(instance.GetValue(i), _element, _converter);
            return Node.Array(items);
        }

        public override Array Instantiate(in FromContext context) =>
            Array.CreateInstance(_element, context.Node.Children.Length);

        public override void Initialize(ref Array instance, in FromContext context)
        {
            var children = context.Node.Children;
            for (int i = 0; i < children.Length; i++)
                instance.SetValue(context.Convert(children[i], _element, _converter), i);
        }
    }
}