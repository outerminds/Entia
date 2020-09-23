using System;
using Entia.Core;

namespace Entia.Json.Converters
{
    public sealed class ConcreteNullable<T> : Converter<T?> where T : struct
    {
        static readonly IConverter _converter = Converter.Default<T>();

        public override Node Convert(in T? instance, in ToContext context) =>
            instance is T value ? context.Convert(value, _converter) : Node.Null;

        public override T? Instantiate(in FromContext context) =>
            context.Node.IsNull() ? Null.None<T>() : context.Convert<T>(context.Node, _converter);
    }

    public sealed class AbstractNullable : IConverter
    {
        public Type Type { get; }

        readonly Type _argument;
        readonly IConverter _converter;

        public AbstractNullable(Type type, Type argument)
        {
            Type = type;
            _argument = argument;
            _converter = Converter.Default(argument);
        }

        public Node Convert(in ToContext context)
        {
            var value = context.Instance;
            if (value is null) return Node.Null;
            return context.Convert(value, _argument, _converter);
        }

        public object Instantiate(in FromContext context)
        {
            if (context.Node.IsNull()) return default;
            return context.Convert(context.Node, _argument, _converter);
        }

        public void Initialize(ref object instance, in FromContext context) { }
    }
}