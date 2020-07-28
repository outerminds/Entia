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
            public override IEnumerable<IConverter> Provide(TypeData type)
            {
                {
                    if (type.Interfaces.TryFirst(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>), out var @interface) &&
                        @interface.GetGenericArguments() is Type[] arguments &&
                        arguments.Select(TypeUtility.GetData).TryFirst(out var element) &&
                        type.InstanceConstructors.TryFirst(constructor =>
                            constructor.GetParameters() is ParameterInfo[] parameters &&
                            parameters.TryFirst(out var parameter) &&
                            element.Array.Type.Is(parameter.ParameterType), out var constructor))
                    {
                        if (Core.Option.Try(() => Activator.CreateInstance(typeof(Enumerable<>).MakeGenericType(element), constructor))
                            .Cast<IConverter>()
                            .TryValue(out var converter))
                            yield return converter;
                        yield return new Converters.Enumerable(element, constructor);
                    }
                }
                {
                    if (type.Interfaces.Contains(typeof(IEnumerable)) &&
                        type.InstanceConstructors.TryFirst(constructor =>
                            constructor.GetParameters() is ParameterInfo[] parameters &&
                            parameters.TryFirst(out var parameter) &&
                            typeof(object[]).Is(parameter.ParameterType), out var constructor))
                        yield return new Converters.Enumerable(typeof(object), constructor);
                }
            }
        }
    }

    public sealed class Enumerable<T> : Converter<IEnumerable<T>>
    {
        readonly ConstructorInfo _constructor;

        public Enumerable(ConstructorInfo constructor) { _constructor = constructor; }

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
            _constructor.Invoke(instance, new object[] { items });
        }
    }

    public sealed class Enumerable : Converter<IEnumerable>
    {
        readonly TypeData _element;
        readonly ConstructorInfo _constructor;

        public Enumerable(TypeData element, ConstructorInfo constructor)
        {
            _element = element;
            _constructor = constructor;
        }

        public override Node Convert(in IEnumerable instance, in ConvertToContext context)
        {
            var items = new List<Node>();
            foreach (var value in instance) items.Add(context.Convert(value, _element));
            return Node.Array(items.ToArray());
        }

        public override IEnumerable Instantiate(in ConvertFromContext context) =>
            FormatterServices.GetUninitializedObject(context.Type) as IEnumerable;

        public override void Initialize(ref IEnumerable instance, in ConvertFromContext context)
        {
            var items = Array.CreateInstance(_element, context.Node.Children.Length);
            for (int i = 0; i < context.Node.Children.Length; i++)
                items.SetValue(context.Convert(context.Node.Children[i], _element), i);
            _constructor.Invoke(instance, new object[] { items });
        }
    }
}