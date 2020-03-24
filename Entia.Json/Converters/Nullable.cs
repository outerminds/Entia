using System;
using System.Collections.Generic;
using Entia.Core;
using Entia.Core.Providers;

namespace Entia.Json.Converters
{
    namespace Providers
    {
        public sealed class Nullable : Provider<IConverter>
        {
            public override IEnumerable<IConverter> Provide(Type type)
            {
                var arguments = type.GetGenericArguments();
                if (Option.Try(arguments, state => Activator.CreateInstance(typeof(ConcreteNullable<>).MakeGenericType(state)))
                    .Cast<IConverter>()
                    .TryValue(out var converter))
                    yield return converter;
                yield return new ConcreteNullable();
            }
        }
    }

    public sealed class ConcreteNullable<T> : Converter<T?> where T : struct
    {
        public override Node Convert(in T? instance, in ConvertToContext context)
        {
            if (instance is T value) return context.Convert(value);
            return Node.Null;
        }

        public override T? Instantiate(in ConvertFromContext context)
        {
            if (context.Node.IsNull()) return default;
            return context.Convert<T>(context.Node);
        }
    }

    public sealed class ConcreteNullable : IConverter
    {
        public bool CanConvert(TypeData type) => type.Definition == typeof(Nullable<>);

        public Node Convert(in ConvertToContext context)
        {
            if (context.Instance is null) return Node.Null;
            return context.Convert(context.Instance, context.Type.Arguments[0]);
        }

        public object Instantiate(in ConvertFromContext context)
        {
            if (context.Node.IsNull()) return default;
            return context.Convert(context.Node, context.Type.Arguments[0]);
        }

        public void Initialize(ref object instance, in ConvertFromContext context) { }
    }
}