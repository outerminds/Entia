using System;
using System.Reflection;
using Entia.Core;

namespace Entia.Json.Converters
{
    public sealed class ConcreteOption<T> : Converter<Option<T>>
    {
        static readonly IConverter _converter = Converter.Default<T>();

        public override Node Convert(in Option<T> instance, in ToContext context)
        {
            if (instance.TryValue(out var value)) return context.Convert(value, _converter);
            return Node.Null;
        }

        public override Option<T> Instantiate(in FromContext context)
        {
            if (context.Node.IsNull()) return Option.None();
            return context.Convert<T>(context.Node, _converter);
        }
    }

    public sealed class AbstractOption : Converter<IOption>
    {
        readonly ConstructorInfo _constructor;
        readonly Type _argument;
        readonly IConverter _converter;

        public AbstractOption(ConstructorInfo constructor, Type argument)
        {
            _constructor = constructor;
            _argument = argument;
            _converter = Converter.Default(argument);
        }

        public override Node Convert(in IOption instance, in ToContext context)
        {
            var value = instance.Value;
            if (value is null) return Node.Null;
            return context.Convert(value, _argument, _converter);
        }

        public override IOption Instantiate(in FromContext context)
        {
            if (context.Node.IsNull()) return default;
            var value = context.Convert(context.Node, _argument, _converter);
            if (value is null) return default;
            return _constructor.Invoke(new object[] { Option.Tags.Some, value }) as IOption;
        }
    }
}