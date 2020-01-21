using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Entia.Core;
using Entia.Core.Providers;

namespace Entia.Json.Converters
{
    namespace Providers
    {
        public sealed class Enumerable : Provider<IConverter>
        {
            public override IEnumerable<IConverter> Provide(Type type)
            {
                if (type.TryElement(out var element) &&
                    Option.Try(element, state => Activator.CreateInstance(typeof(Enumerable<>).MakeGenericType(state)))
                    .Cast<IConverter>()
                    .TryValue(out var converter))
                    yield return converter;
                yield return new Converters.Enumerable();
            }
        }
    }

    public sealed class Enumerable<T> : Converter<IEnumerable<T>>
    {
        public override bool CanConvert(TypeData type) =>
            type.EnumerableConstructor.constructor is ConstructorInfo &&
            type.Element.ArrayType.Type.Is(type.EnumerableConstructor.parameter.ParameterType);

        public override Node Convert(in IEnumerable<T> instance, in ConvertToContext context)
        {
            var items = new List<Node>();
            foreach (var value in instance) items.Add(context.Convert(value));
            return Node.Array(items.ToArray());
        }

        public override IEnumerable<T> Instantiate(in ConvertFromContext context) =>
            FormatterServices.GetUninitializedObject(context.Type) as IEnumerable<T>;

        public override void Initialize(ref IEnumerable<T> instance, in ConvertFromContext context)
        {
            var items = new T[context.Node.Children.Length];
            for (int i = 0; i < context.Node.Children.Length; i++)
                items[i] = context.Convert<T>(context.Node.Children[i]);
            context.Type.EnumerableConstructor.constructor.Invoke(instance, new object[] { items });
        }
    }

    public sealed class Enumerable : Converter<IEnumerable>
    {
        static readonly TypeData _default = TypeUtility.GetData<object>();

        public override bool CanConvert(TypeData type) =>
            type.EnumerableConstructor.constructor is ConstructorInfo &&
            (type.Element?.ArrayType.Type ?? _default.ArrayType.Type).Is(type.EnumerableConstructor.parameter.ParameterType);

        public override Node Convert(in IEnumerable instance, in ConvertToContext context)
        {
            var items = new List<Node>();
            var element = context.Type.Element ?? _default;
            foreach (var value in instance) items.Add(context.Convert(value, element));
            return Node.Array(items.ToArray());
        }

        public override IEnumerable Instantiate(in ConvertFromContext context) =>
            FormatterServices.GetUninitializedObject(context.Type) as IEnumerable;

        public override void Initialize(ref IEnumerable instance, in ConvertFromContext context)
        {
            var element = context.Type.Element ?? _default;
            var items = Array.CreateInstance(element, context.Node.Children.Length);
            for (int i = 0; i < context.Node.Children.Length; i++)
                items.SetValue(context.Convert(context.Node.Children[i], element), i);
            context.Type.EnumerableConstructor.constructor.Invoke(instance, new object[] { items });
        }
    }
}