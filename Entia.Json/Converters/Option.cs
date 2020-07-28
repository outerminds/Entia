using System;
using System.Collections.Generic;
using System.Reflection;
using Entia.Core;
using Entia.Core.Providers;

namespace Entia.Json.Converters
{
    namespace Providers
    {
        public sealed class Option : Provider<IConverter>
        {
            public override IEnumerable<IConverter> Provide(TypeData type)
            {
                if (type.Definition == typeof(Option<>) && type.Arguments.TryFirst(out var argument))
                {
                    if (Core.Option.Try(argument, state => Activator.CreateInstance(typeof(ConcreteOption<>).MakeGenericType(state)))
                        .Cast<IConverter>()
                        .TryValue(out var converter))
                        yield return converter;
                    if (type.InstanceConstructors.TryFirst(current =>
                        current.GetParameters() is ParameterInfo[] parameters &&
                        parameters.Length == 2, out var constructor))
                        yield return new AbstractOption(constructor, argument);
                }
            }
        }
    }

    public sealed class ConcreteOption<T> : Converter<Option<T>>
    {
        public override Node Convert(in Option<T> instance, in ConvertToContext context)
        {
            if (instance.TryValue(out var value)) return context.Convert(value);
            return Node.Null;
        }

        public override Option<T> Instantiate(in ConvertFromContext context)
        {
            if (context.Node.IsNull()) return Option.None();
            return context.Convert<T>(context.Node);
        }
    }

    public sealed class AbstractOption : Converter<IOption>
    {
        readonly ConstructorInfo _constructor;
        readonly TypeData _argument;

        public AbstractOption(ConstructorInfo constructor, TypeData argument)
        {
            _constructor = constructor;
            _argument = argument;
        }

        public override Node Convert(in IOption instance, in ConvertToContext context)
        {
            var value = instance.Value;
            if (value is null) return Node.Null;
            return context.Convert(value, _argument);
        }

        public override IOption Instantiate(in ConvertFromContext context)
        {
            if (context.Node.IsNull()) return default;
            var value = context.Convert(context.Node, _argument);
            if (value is null) return default;
            return _constructor.Invoke(new object[] { Option.Tags.Some, value }) as IOption;
        }
    }
}