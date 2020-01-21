using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Entia.Core;

namespace Entia.Json.Converters
{
    public sealed class AbstractType : Converter<Type>
    {
        [Preserve]
        readonly struct Members
        {
            [Preserve] public readonly object Field;
            [Preserve] public object Property { get; }
            [Preserve] public void Method() { }

            [Preserve]
            public Members(object field, object property) { Field = field; Property = property; Method(); }
        }

        enum Kinds { Type = 1, Array = 2, Pointer = 3, Generic = 4 }

        static readonly Type[] _types =
        {
            #region System
            typeof(bool), typeof(bool[]), typeof(bool*), typeof(bool?),
            typeof(char), typeof(char[]), typeof(char*), typeof(char?),
            typeof(byte), typeof(byte[]), typeof(byte*), typeof(byte?),
            typeof(sbyte), typeof(sbyte[]), typeof(sbyte*), typeof(sbyte?),
            typeof(short), typeof(short[]), typeof(short*), typeof(short?),
            typeof(ushort), typeof(ushort[]), typeof(ushort*), typeof(ushort?),
            typeof(int), typeof(int[]), typeof(int*), typeof(int?),
            typeof(uint), typeof(uint[]), typeof(uint*), typeof(uint?),
            typeof(long), typeof(long[]), typeof(long*), typeof(long?),
            typeof(ulong), typeof(ulong[]), typeof(ulong*), typeof(ulong?),
            typeof(float), typeof(float[]), typeof(float*), typeof(float?),
            typeof(double), typeof(double[]), typeof(double*), typeof(double?),
            typeof(decimal), typeof(decimal[]), typeof(decimal*), typeof(decimal?),
            typeof(IntPtr), typeof(IntPtr[]), typeof(IntPtr*), typeof(IntPtr?),
            typeof(DateTime), typeof(DateTime[]), typeof(DateTime*), typeof(DateTime?),
            typeof(TimeSpan), typeof(TimeSpan[]), typeof(TimeSpan*), typeof(TimeSpan?),
            typeof(string), typeof(string[]),
            typeof(Action), typeof(Action[]),
            typeof(object), typeof(object[]),

            typeof(Nullable<>),
            typeof(List<>), typeof(LinkedList<>), typeof(LinkedListNode<>),
            typeof(Stack<>), typeof(Queue<>), typeof(HashSet<>), typeof(Dictionary<,>), typeof(KeyValuePair<,>),
            typeof(SortedDictionary<,>), typeof(SortedList<,>), typeof(SortedSet<>),
            typeof(Tuple<>), typeof(Tuple<,>), typeof(Tuple<,,>), typeof(Tuple<,,,>), typeof(Tuple<,,,,>), typeof(Tuple<,,,,,>), typeof(Tuple<,,,,,,>),
            typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>), typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>),
            typeof(Action<>), typeof(Action<,>), typeof(Action<,,>), typeof(Action<,,,>), typeof(Action<,,,,>), typeof(Action<,,,,,>), typeof(Action<,,,,,,>),
            typeof(Func<>), typeof(Func<,>), typeof(Func<,,>), typeof(Func<,,,>), typeof(Func<,,,,>), typeof(Func<,,,,,>), typeof(Func<,,,,,,>),
            typeof(Predicate<>), typeof(Comparison<>),
            #endregion

            #region Reflection
            typeof(object).GetType(), typeof(object).Module.GetType(), typeof(object).Assembly.GetType(),
            typeof(Members).GetField(nameof(Members.Field)).GetType(),
            typeof(Members).GetProperty(nameof(Members.Property)).GetType(),
            typeof(Members).GetMethod(nameof(Members.Method)).GetType(),
            typeof(Pointer),
            #endregion
        };
        static readonly Dictionary<Type, int> _indices = _types
            .Select((type, index) => (type, index))
            .ToDictionary(pair => pair.type, pair => pair.index);

        public override Node Convert(in Type instance, in ConvertToContext context)
        {
            if (_indices.TryGetValue(instance, out var index)) return Node.Number(index);
            else if (instance.IsArray) return Node.Array(
                (int)Kinds.Array,
                instance.GetArrayRank(),
                context.Convert(instance.GetElementType()));
            else if (instance.IsPointer) return Node.Array(
                (int)Kinds.Pointer,
                context.Convert(instance.GetElementType()));
            else if (instance.IsConstructedGenericType)
            {
                var definition = instance.GetGenericTypeDefinition();
                var arguments = instance.GetGenericArguments();
                var items = new Node[arguments.Length + 2];
                items[0] = (int)Kinds.Generic;
                items[1] = context.Convert(definition);
                for (int i = 0; i < arguments.Length; i++)
                    items[i + 2] = context.Convert(arguments[i]);
                return Node.Array(items);
            }
            else if (TypeUtility.TryGetGuid(instance, out var guid)) return guid.ToString();
            else return Node.Array(Node.Number((int)Kinds.Type), instance.FullName);
        }

        public override Type Instantiate(in ConvertFromContext context)
        {
            var node = context.Node;
            switch (node.Kind)
            {
                case Node.Kinds.Number:
                    return _types[node.AsInt()];
                case Node.Kinds.String:
                    {
                        var value = node.AsString();
                        if (Guid.TryParse(value, out var guid) && TypeUtility.TryGetType(guid, out var type)) return type;
                        else if (TypeUtility.TryGetType(value, out type)) return type;
                        return default;
                    }
                case Node.Kinds.Array:
                    switch ((Kinds)node.Children[0].AsInt())
                    {
                        case Kinds.Type:
                            return TypeUtility.TryGetType(node.Children[1].AsString(), out var value) ? value : default;
                        case Kinds.Array:
                            var rank = node.Children[1].AsInt();
                            var element = context.Convert<Type>(node.Children[2]);
                            return rank > 1 ? element.MakeArrayType(rank) : element.MakeArrayType();
                        case Kinds.Pointer:
                            var pointer = context.Convert<Type>(node.Children[1]);
                            return pointer.MakePointerType();
                        case Kinds.Generic:
                            var definition = context.Convert<Type>(node.Children[1]);
                            var arguments = new Type[node.Children.Length - 2];
                            for (int i = 0; i < arguments.Length; i++)
                                arguments[i] = context.Convert<Type>(node.Children[i + 2]);
                            return definition.MakeGenericType(arguments);
                    }
                    break;
            }
            return default;
        }
    }
}