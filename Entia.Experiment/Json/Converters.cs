using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Entia.Core;
using Entia.Core.Providers;

namespace Entia.Experiment.Json.Converters
{
    [Implementation(typeof(Node), typeof(ConcreteNode))]
    [Implementation(typeof(Type), typeof(AbstractType))]
    [Implementation(typeof(DateTime), typeof(ConcreteDateTime))]
    [Implementation(typeof(TimeSpan), typeof(ConcreteTimeSpan))]
    [Implementation(typeof(Array), typeof(Providers.Array))]
    [Implementation(typeof(List<>), typeof(Providers.List))]
    [Implementation(typeof(IDictionary<,>), typeof(Providers.Dictionary))]
    [Implementation(typeof(IDictionary), typeof(AbstractDictionary))]
    [Implementation(typeof(IEnumerable<>), typeof(Providers.Enumerable))]
    [Implementation(typeof(IEnumerable), typeof(AbstractEnumerable))]
    [Implementation(typeof(ISerializable), typeof(AbstractSerializable))]
    public interface IConverter : ITrait
    {
        bool CanConvert(TypeData type);
        Node Convert(in ConvertToContext context);
        object Instantiate(in ConvertFromContext context);
        void Initialize(ref object instance, in ConvertFromContext context);
    }

    public abstract class Converter<T> : IConverter
    {
        public virtual bool CanConvert(TypeData type) => true;
        public abstract Node Convert(in T instance, in ConvertToContext context);
        public abstract T Instantiate(in ConvertFromContext context);
        public virtual void Initialize(ref T instance, in ConvertFromContext context) { }

        Node IConverter.Convert(in ConvertToContext context) =>
            context.Instance is T casted ? Convert(casted, context) : Node.Null;
        object IConverter.Instantiate(in ConvertFromContext context) => Instantiate(context);
        void IConverter.Initialize(ref object instance, in ConvertFromContext context)
        {
            if (instance is T casted)
            {
                Initialize(ref casted, context);
                instance = casted;
            }
        }
    }

    namespace Providers
    {
        public sealed class Array : Provider<IConverter>
        {
            public override IEnumerable<IConverter> Provide(Type type)
            {
                var element = type.GetElementType();
                switch (Type.GetTypeCode(element))
                {
                    case TypeCode.Char: yield return new PrimitiveArray<char>(Node.Number, node => char.Parse(node.Value)); break;
                    case TypeCode.Byte: yield return new PrimitiveArray<byte>(Node.Number, node => byte.Parse(node.Value)); break;
                    case TypeCode.SByte: yield return new PrimitiveArray<sbyte>(Node.Number, node => sbyte.Parse(node.Value)); break;
                    case TypeCode.Int16: yield return new PrimitiveArray<short>(Node.Number, node => short.Parse(node.Value)); break;
                    case TypeCode.Int32: yield return new PrimitiveArray<int>(Node.Number, node => int.Parse(node.Value)); break;
                    case TypeCode.Int64: yield return new PrimitiveArray<long>(Node.Number, node => long.Parse(node.Value)); break;
                    case TypeCode.UInt16: yield return new PrimitiveArray<ushort>(Node.Number, node => ushort.Parse(node.Value)); break;
                    case TypeCode.UInt32: yield return new PrimitiveArray<uint>(Node.Number, node => uint.Parse(node.Value)); break;
                    case TypeCode.UInt64: yield return new PrimitiveArray<ulong>(Node.Number, node => ulong.Parse(node.Value)); break;
                    case TypeCode.Single: yield return new PrimitiveArray<float>(Node.Number, node => float.Parse(node.Value)); break;
                    case TypeCode.Double: yield return new PrimitiveArray<double>(Node.Number, node => double.Parse(node.Value)); break;
                    case TypeCode.Decimal: yield return new PrimitiveArray<decimal>(Node.Number, node => decimal.Parse(node.Value)); break;
                    case TypeCode.Boolean: yield return new PrimitiveArray<bool>(Node.Boolean, node => bool.Parse(node.Value)); break;
                    case TypeCode.String: yield return new PrimitiveArray<string>(Node.String, node => node.Value); break;
                }

                if (Option.Try(element, state => Activator.CreateInstance(typeof(ConcreteArray<>).MakeGenericType(state)))
                    .Cast<IConverter>()
                    .TryValue(out var converter))
                    yield return converter;
                yield return new AbstractArray();
            }
        }

        public sealed class List : Provider<IConverter>
        {
            public override IEnumerable<IConverter> Provide(Type type)
            {
                var arguments = type.GetGenericArguments();
                switch (Type.GetTypeCode(arguments[0]))
                {
                    case TypeCode.Char: yield return new PrimitiveList<char>(Node.Number, node => char.Parse(node.Value)); break;
                    case TypeCode.Byte: yield return new PrimitiveList<byte>(Node.Number, node => byte.Parse(node.Value)); break;
                    case TypeCode.SByte: yield return new PrimitiveList<sbyte>(Node.Number, node => sbyte.Parse(node.Value)); break;
                    case TypeCode.Int16: yield return new PrimitiveList<short>(Node.Number, node => short.Parse(node.Value)); break;
                    case TypeCode.Int32: yield return new PrimitiveList<int>(Node.Number, node => int.Parse(node.Value)); break;
                    case TypeCode.Int64: yield return new PrimitiveList<long>(Node.Number, node => long.Parse(node.Value)); break;
                    case TypeCode.UInt16: yield return new PrimitiveList<ushort>(Node.Number, node => ushort.Parse(node.Value)); break;
                    case TypeCode.UInt32: yield return new PrimitiveList<uint>(Node.Number, node => uint.Parse(node.Value)); break;
                    case TypeCode.UInt64: yield return new PrimitiveList<ulong>(Node.Number, node => ulong.Parse(node.Value)); break;
                    case TypeCode.Single: yield return new PrimitiveList<float>(Node.Number, node => float.Parse(node.Value)); break;
                    case TypeCode.Double: yield return new PrimitiveList<double>(Node.Number, node => double.Parse(node.Value)); break;
                    case TypeCode.Decimal: yield return new PrimitiveList<decimal>(Node.Number, node => decimal.Parse(node.Value)); break;
                    case TypeCode.Boolean: yield return new PrimitiveList<bool>(Node.Boolean, node => bool.Parse(node.Value)); break;
                    case TypeCode.String: yield return new PrimitiveList<string>(Node.String, node => node.Value); break;
                }

                if (Option.Try(arguments, state => Activator.CreateInstance(typeof(ConcreteList<>).MakeGenericType(state)))
                    .Cast<IConverter>()
                    .TryValue(out var converter))
                    yield return converter;
                yield return new AbstractList();
            }
        }

        public sealed class Dictionary : Provider<IConverter>
        {
            public override IEnumerable<IConverter> Provide(Type type)
            {
                var arguments = type.GetGenericArguments();
                if (type.Is(typeof(Dictionary<,>), definition: true) &&
                    Option.Try(arguments, state => Activator.CreateInstance(typeof(ConcreteDictionary<,>).MakeGenericType(state)))
                    .Cast<IConverter>()
                    .TryValue(out var converter))
                    yield return converter;
                if (Option.Try(arguments, state => Activator.CreateInstance(typeof(AbstractDictionary<,>).MakeGenericType(state)))
                    .Cast<IConverter>()
                    .TryValue(out converter))
                    yield return converter;
                yield return new AbstractDictionary(arguments[0], arguments[1]);
            }
        }

        public sealed class Enumerable : Provider<IConverter>
        {
            public override IEnumerable<IConverter> Provide(Type type)
            {
                var arguments = type.GetGenericArguments();
                if (Option.Try(arguments, state => Activator.CreateInstance(typeof(AbstractEnumerable<>).MakeGenericType(state)))
                    .Cast<IConverter>()
                    .TryValue(out var converter))
                    yield return converter;
                yield return new AbstractEnumerable();
            }
        }
    }

    public sealed class ConcreteNode : Converter<Node>
    {
        public override Node Convert(in Node instance, in ConvertToContext context) => instance;
        public override Node Instantiate(in ConvertFromContext context) => context.Node;
    }

    public sealed class ConcreteDateTime : Converter<DateTime>
    {
        public override Node Convert(in DateTime instance, in ConvertToContext context) =>
            Node.Array(instance.Ticks, (int)instance.Kind);
        public override DateTime Instantiate(in ConvertFromContext context) => new DateTime(
            long.Parse(context.Node.Children[0].Value),
            (DateTimeKind)int.Parse(context.Node.Children[1].Value));
    }

    public sealed class ConcreteTimeSpan : Converter<TimeSpan>
    {
        public override Node Convert(in TimeSpan instance, in ConvertToContext context) => instance.Ticks;
        public override TimeSpan Instantiate(in ConvertFromContext context) => new TimeSpan(long.Parse(context.Node.Value));
    }

    public sealed class AbstractSerializable : Converter<ISerializable>
    {
        static readonly FormatterConverter _converter = new FormatterConverter();
        static readonly StreamingContext _context = new StreamingContext(StreamingContextStates.All);

        public override bool CanConvert(TypeData type) => type.SerializationConstructor is ConstructorInfo;

        public override Node Convert(in ISerializable instance, in ConvertToContext context)
        {
            var info = new SerializationInfo(context.Type, _converter);
            instance.GetObjectData(info, _context);
            var members = new Node[info.MemberCount];
            var index = 0;
            foreach (var pair in info)
                members[index++] = Node.Member(pair.Name, context.Convert(pair.Value));
            return Node.Object(members);
        }

        public override ISerializable Instantiate(in ConvertFromContext context) =>
            FormatterServices.GetUninitializedObject(context.Type) as ISerializable;

        public override void Initialize(ref ISerializable instance, in ConvertFromContext context)
        {
            var info = new SerializationInfo(context.Type, _converter);
            foreach (var member in context.Node.Children)
            {
                if (member.TryMember(out var key, out var value))
                    info.AddValue(key.Value, context.Convert<object>(value));
            }
            context.Type.SerializationConstructor.Invoke(instance, new object[] { info, _context });
            if (instance is IDeserializationCallback callback) callback.OnDeserialization(this);
        }
    }

    public sealed class AbstractEnumerable<T> : Converter<IEnumerable<T>>
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

    public sealed class AbstractEnumerable : Converter<IEnumerable>
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
        public override bool CanConvert(TypeData type) => type.Element is TypeData;

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

    public sealed class ConcreteDictionary<TKey, TValue> : Converter<Dictionary<TKey, TValue>>
    {
        static readonly bool _convertible = typeof(TKey).Is<IConvertible>();

        public override Node Convert(in Dictionary<TKey, TValue> instance, in ConvertToContext context)
        {
            var members = new Node[instance.Count];
            var index = 0;
            foreach (var pair in instance)
                members[index++] = Node.Array(context.Convert(pair.Key), context.Convert(pair.Value));
            return Node.Array(members);
        }

        public override Dictionary<TKey, TValue> Instantiate(in ConvertFromContext context) =>
            new Dictionary<TKey, TValue>(context.Node.Children.Length);

        public override void Initialize(ref Dictionary<TKey, TValue> instance, in ConvertFromContext context)
        {
            foreach (var member in context.Node.Children)
            {
                if (member.TryItem(0, out var key) && member.TryItem(1, out var value))
                    instance.Add(context.Convert<TKey>(key), context.Convert<TValue>(value));
            }
        }
    }

    public sealed class AbstractDictionary<TKey, TValue> : Converter<IDictionary<TKey, TValue>>
    {
        public override bool CanConvert(TypeData type) => type.DefaultConstructor is ConstructorInfo;

        public override Node Convert(in IDictionary<TKey, TValue> instance, in ConvertToContext context)
        {
            var members = new Node[instance.Count];
            var index = 0;
            foreach (var pair in instance)
                members[index++] = Node.Array(context.Convert(pair.Key), context.Convert(pair.Value));
            return Node.Array(members);
        }

        public override IDictionary<TKey, TValue> Instantiate(in ConvertFromContext context) =>
            context.Type.DefaultConstructor.Invoke(Array.Empty<object>()) as IDictionary<TKey, TValue>;

        public override void Initialize(ref IDictionary<TKey, TValue> instance, in ConvertFromContext context)
        {
            foreach (var member in context.Node.Children)
            {
                if (member.TryItem(0, out var key) && member.TryItem(1, out var value))
                    instance.Add(context.Convert<TKey>(key), context.Convert<TValue>(value));
            }
        }
    }

    public sealed class AbstractDictionary : Converter<IDictionary>
    {
        readonly Type _key;
        readonly Type _value;

        public AbstractDictionary() : this(typeof(object), typeof(object)) { }
        public AbstractDictionary(Type key, Type value)
        {
            _key = key;
            _value = value;
        }

        public override bool CanConvert(TypeData type) => type.DefaultConstructor is ConstructorInfo;

        public override Node Convert(in IDictionary instance, in ConvertToContext context)
        {
            var members = new Node[instance.Count];
            var index = 0;
            foreach (var key in instance.Keys)
                members[index++] = Node.Array(context.Convert(key, _key), context.Convert(instance[key], _value));
            return Node.Array(members);
        }

        public override IDictionary Instantiate(in ConvertFromContext context) =>
            context.Type.DefaultConstructor.Invoke(Array.Empty<object>()) as IDictionary;

        public override void Initialize(ref IDictionary instance, in ConvertFromContext context)
        {
            foreach (var member in context.Node.Children)
            {
                if (member.TryItem(0, out var key) && member.TryItem(1, out var value))
                    instance.Add(context.Convert(key, _key), context.Convert(value, _value));
            }
        }
    }

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

            #region Entia.Core
            typeof(Unit), typeof(Unit[]), typeof(Unit*), typeof(Unit?),
            typeof(BitMask), typeof(Disposable),

            typeof(Concurrent<>),
            typeof(Option<>), typeof(Result<>),
            typeof(Box<>), typeof(Box<>.Read),
            typeof(Slice<>), typeof(Slice<>.Read),
            typeof(TypeMap<,>),
            typeof(Disposable<>),
            #endregion
        };
        static readonly Dictionary<Type, int> _indices = _types
            .Select((type, index) => (type, index))
            .ToDictionary(pair => pair.type, pair => pair.index);

        public override Node Convert(in Type instance, in ConvertToContext context)
        {
            if (_indices.TryGetValue(instance, out var index)) return Node.Number(index);
            else if (instance.IsArray) return Node.Array(
                Node.Number((int)Kinds.Array),
                Node.Number(instance.GetArrayRank()),
                context.Convert(instance.GetElementType()));
            else if (instance.IsPointer) return Node.Array(
                Node.Number((int)Kinds.Pointer),
                context.Convert(instance.GetElementType()));
            else if (instance.IsConstructedGenericType)
            {
                var definition = instance.GetGenericTypeDefinition();
                var arguments = instance.GetGenericArguments();
                var items = new Node[arguments.Length + 2];
                items[0] = Node.Number((int)Kinds.Generic);
                items[1] = context.Convert(definition);
                for (int i = 0; i < arguments.Length; i++)
                    items[i + 2] = context.Convert(arguments[i]);
                return Node.Array(items);
            }
            else if (TypeUtility.TryGetGuid(instance, out var guid)) return Node.String(guid.ToString());
            else return Node.Array(Node.Number((int)Kinds.Type), Node.String(instance.FullName));

        }

        public override Type Instantiate(in ConvertFromContext context)
        {
            var node = context.Node;
            switch (node.Kind)
            {
                case Node.Kinds.Number:
                    return _types[int.Parse(node.Value)];
                case Node.Kinds.String:
                    if (Guid.TryParse(node.Value, out var guid) && TypeUtility.TryGetType(guid, out var type))
                        return type;
                    return default;
                case Node.Kinds.Array:
                    switch (Enum.Parse(typeof(Kinds), node.Children[0].Value))
                    {
                        case Kinds.Type:
                            return TypeUtility.TryGetType(node.Children[1].Value, out var value) ? value : default;
                        case Kinds.Array:
                            var rank = int.Parse(node.Children[1].Value);
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