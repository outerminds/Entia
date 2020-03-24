using System;
using System.Collections;
using System.Collections.Generic;
using Entia.Core;
using Entia.Core.Providers;

namespace Entia.Json.Converters
{
    namespace Providers
    {
        public sealed class List : Provider<IConverter>
        {
            public override IEnumerable<IConverter> Provide(Type type)
            {
                var arguments = type.GetGenericArguments();
                switch (Type.GetTypeCode(arguments[0]))
                {
                    case TypeCode.Char: yield return new PrimitiveList<char>(_ => _, node => node.AsChar()); break;
                    case TypeCode.Byte: yield return new PrimitiveList<byte>(_ => _, node => node.AsByte()); break;
                    case TypeCode.SByte: yield return new PrimitiveList<sbyte>(_ => _, node => node.AsSByte()); break;
                    case TypeCode.Int16: yield return new PrimitiveList<short>(_ => _, node => node.AsShort()); break;
                    case TypeCode.Int32: yield return new PrimitiveList<int>(_ => _, node => node.AsInt()); break;
                    case TypeCode.Int64: yield return new PrimitiveList<long>(_ => _, node => node.AsLong()); break;
                    case TypeCode.UInt16: yield return new PrimitiveList<ushort>(_ => _, node => node.AsUShort()); break;
                    case TypeCode.UInt32: yield return new PrimitiveList<uint>(_ => _, node => node.AsUInt()); break;
                    case TypeCode.UInt64: yield return new PrimitiveList<ulong>(_ => _, node => node.AsULong()); break;
                    case TypeCode.Single: yield return new PrimitiveList<float>(_ => _, node => node.AsFloat()); break;
                    case TypeCode.Double: yield return new PrimitiveList<double>(_ => _, node => node.AsDouble()); break;
                    case TypeCode.Decimal: yield return new PrimitiveList<decimal>(_ => _, node => node.AsDecimal()); break;
                    case TypeCode.Boolean: yield return new PrimitiveList<bool>(_ => _, node => node.AsBool()); break;
                    case TypeCode.String: yield return new PrimitiveList<string>(_ => _, node => node.AsString()); break;
                }

                if (Option.Try(arguments, state => Activator.CreateInstance(typeof(ConcreteList<>).MakeGenericType(state)))
                    .Cast<IConverter>()
                    .TryValue(out var converter))
                    yield return converter;
                yield return new AbstractList();
            }
        }
    }

    public sealed class PrimitiveList<T> : Converter<List<T>>
    {
        readonly Func<T, Node> _to;
        readonly Func<Node, T> _from;

        public PrimitiveList(Func<T, Node> to, Func<Node, T> from)
        {
            _to = to;
            _from = from;
        }

        public override Node Convert(in List<T> instance, in ConvertToContext context)
        {
            var items = new Node[instance.Count];
            for (int i = 0; i < items.Length; i++) items[i] = _to(instance[i]);
            return Node.Array(items);
        }

        public override List<T> Instantiate(in ConvertFromContext context) => new List<T>(context.Node.Children.Length);

        public override void Initialize(ref List<T> instance, in ConvertFromContext context)
        {
            for (int i = 0; i < context.Node.Children.Length; i++)
                instance.Add(_from(context.Node.Children[i]));
        }
    }

    public sealed class ConcreteList<T> : Converter<List<T>>
    {
        public override Node Convert(in List<T> instance, in ConvertToContext context)
        {
            var items = new Node[instance.Count];
            for (int i = 0; i < items.Length; i++) items[i] = context.Convert(instance[i]);
            return Node.Array(items);
        }

        public override List<T> Instantiate(in ConvertFromContext context) => new List<T>(context.Node.Children.Length);

        public override void Initialize(ref List<T> instance, in ConvertFromContext context)
        {
            for (int i = 0; i < context.Node.Children.Length; i++)
                instance.Add(context.Convert<T>(context.Node.Children[i]));
        }
    }

    public sealed class AbstractList : Converter<IList>
    {
        public override bool CanConvert(TypeData type) => type.Element is TypeData;

        public override Node Convert(in IList instance, in ConvertToContext context)
        {
            var element = context.Type.Element;
            var items = new Node[instance.Count];
            for (int i = 0; i < items.Length; i++) items[i] = context.Convert(instance[i], element);
            return Node.Array(items);
        }

        public override IList Instantiate(in ConvertFromContext context) =>
            Activator.CreateInstance(context.Type, new object[] { context.Node.Children.Length }) as IList;

        public override void Initialize(ref IList instance, in ConvertFromContext context)
        {
            var element = context.Type.Element;
            for (int i = 0; i < context.Node.Children.Length; i++)
                instance.Add(context.Convert(context.Node.Children[i], element));
        }
    }
}