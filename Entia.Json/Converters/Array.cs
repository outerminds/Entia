using System;
using System.Collections.Generic;
using Entia.Core;
using Entia.Core.Providers;

namespace Entia.Json.Converters
{
    namespace Providers
    {
        public sealed class Array : Provider<IConverter>
        {
            public override IEnumerable<IConverter> Provide(Type type)
            {
                var element = type.GetElementType();
                switch (Type.GetTypeCode(element))
                {
                    case TypeCode.Char: yield return new PrimitiveArray<char>(_ => _, node => node.AsChar()); break;
                    case TypeCode.Byte: yield return new PrimitiveArray<byte>(_ => _, node => node.AsByte()); break;
                    case TypeCode.SByte: yield return new PrimitiveArray<sbyte>(_ => _, node => node.AsSByte()); break;
                    case TypeCode.Int16: yield return new PrimitiveArray<short>(_ => _, node => node.AsShort()); break;
                    case TypeCode.Int32: yield return new PrimitiveArray<int>(_ => _, node => node.AsInt()); break;
                    case TypeCode.Int64: yield return new PrimitiveArray<long>(_ => _, node => node.AsLong()); break;
                    case TypeCode.UInt16: yield return new PrimitiveArray<ushort>(_ => _, node => node.AsUShort()); break;
                    case TypeCode.UInt32: yield return new PrimitiveArray<uint>(_ => _, node => node.AsUInt()); break;
                    case TypeCode.UInt64: yield return new PrimitiveArray<ulong>(_ => _, node => node.AsULong()); break;
                    case TypeCode.Single: yield return new PrimitiveArray<float>(_ => _, node => node.AsFloat()); break;
                    case TypeCode.Double: yield return new PrimitiveArray<double>(_ => _, node => node.AsDouble()); break;
                    case TypeCode.Decimal: yield return new PrimitiveArray<decimal>(_ => _, node => node.AsDecimal()); break;
                    case TypeCode.Boolean: yield return new PrimitiveArray<bool>(_ => _, node => node.AsBool()); break;
                    case TypeCode.String: yield return new PrimitiveArray<string>(_ => _, node => node.AsString()); break;
                }

                if (Option.Try(element, state => Activator.CreateInstance(typeof(ConcreteArray<>).MakeGenericType(state)))
                    .Cast<IConverter>()
                    .TryValue(out var converter))
                    yield return converter;
                yield return new AbstractArray();
            }
        }
    }

    public sealed class PrimitiveArray<T> : Converter<T[]>
    {
        readonly Func<T, Node> _to;
        readonly Func<Node, T> _from;

        public PrimitiveArray(Func<T, Node> to, Func<Node, T> from)
        {
            _to = to;
            _from = from;
        }

        public override Node Convert(in T[] instance, in ConvertToContext context)
        {
            var items = new Node[instance.Length];
            for (int i = 0; i < instance.Length; i++) items[i] = _to(instance[i]);
            return Node.Array(items);
        }

        public override T[] Instantiate(in ConvertFromContext context) => new T[context.Node.Children.Length];

        public override void Initialize(ref T[] instance, in ConvertFromContext context)
        {
            for (int i = 0; i < instance.Length; i++)
                instance[i] = _from(context.Node.Children[i]);
        }
    }

    public sealed class ConcreteArray<T> : Converter<T[]>
    {
        public override Node Convert(in T[] instance, in ConvertToContext context)
        {
            var items = new Node[instance.Length];
            for (int i = 0; i < instance.Length; i++) items[i] = context.Convert(instance[i]);
            return Node.Array(items);
        }

        public override T[] Instantiate(in ConvertFromContext context) => new T[context.Node.Children.Length];

        public override void Initialize(ref T[] instance, in ConvertFromContext context)
        {
            for (int i = 0; i < context.Node.Children.Length; i++)
                instance[i] = context.Convert<T>(context.Node.Children[i]);
        }
    }

    public sealed class AbstractArray : Converter<Array>
    {
        public override bool Validate(TypeData type) => type.Element is TypeData;

        public override Node Convert(in Array instance, in ConvertToContext context)
        {
            var element = context.Type.Element;
            var items = new Node[instance.Length];
            for (int i = 0; i < instance.Length; i++) items[i] = context.Convert(instance.GetValue(i), element);
            return Node.Array(items);
        }

        public override Array Instantiate(in ConvertFromContext context) =>
            Array.CreateInstance(context.Type.Element, context.Node.Children.Length);

        public override void Initialize(ref Array instance, in ConvertFromContext context)
        {
            var element = context.Type.Element;
            for (int i = 0; i < context.Node.Children.Length; i++)
                instance.SetValue(context.Convert(context.Node.Children[i], element), i);
        }
    }
}