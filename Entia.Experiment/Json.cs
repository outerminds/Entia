using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Entia.Core;

namespace Entia.Experiment
{
    public static class Json
    {
        public enum Kinds { Null, Boolean, Number, String, Object, Member, Array }
        public enum Format { Compact, Indented }

        public readonly struct Node
        {
            public static implicit operator Node(bool value) => Boolean(value);
            public static implicit operator Node(char value) => Number(value);
            public static implicit operator Node(byte value) => Number(value);
            public static implicit operator Node(sbyte value) => Number(value);
            public static implicit operator Node(short value) => Number(value);
            public static implicit operator Node(ushort value) => Number(value);
            public static implicit operator Node(int value) => Number(value);
            public static implicit operator Node(uint value) => Number(value);
            public static implicit operator Node(long value) => Number(value);
            public static implicit operator Node(ulong value) => Number(value);
            public static implicit operator Node(float value) => Number(value);
            public static implicit operator Node(double value) => Number(value);
            public static implicit operator Node(decimal value) => Number(value);
            public static implicit operator Node(Enum value) => Number(value);
            public static implicit operator Node(string value) => String(value);

            public static readonly Node Null = new Node(Kinds.Null, "null");
            public static readonly Node True = new Node(Kinds.Null, bool.TrueString.ToLower());
            public static readonly Node False = new Node(Kinds.Null, bool.FalseString.ToLower());
            public static Node Boolean(bool value) => value ? True : False;
            public static Node Number(char value) => new Node(Kinds.Number, value.ToString());
            public static Node Number(byte value) => new Node(Kinds.Number, value.ToString());
            public static Node Number(sbyte value) => new Node(Kinds.Number, value.ToString());
            public static Node Number(short value) => new Node(Kinds.Number, value.ToString());
            public static Node Number(ushort value) => new Node(Kinds.Number, value.ToString());
            public static Node Number(int value) => new Node(Kinds.Number, value.ToString());
            public static Node Number(uint value) => new Node(Kinds.Number, value.ToString());
            public static Node Number(long value) => new Node(Kinds.Number, value.ToString());
            public static Node Number(ulong value) => new Node(Kinds.Number, value.ToString());
            public static Node Number(float value) => new Node(Kinds.Number, value.ToString());
            public static Node Number(double value) => new Node(Kinds.Number, value.ToString());
            public static Node Number(decimal value) => new Node(Kinds.Number, value.ToString());
            public static Node Number(Enum value) => new Node(Kinds.Number, System.Convert.ToDecimal(value));
            public static Node String(string value) => new Node(Kinds.String, value);
            public static Node Member(Node key, Node value) => new Node(Kinds.Member, key, value);
            public static Node Array(params Node[] items) => new Node(Kinds.Array, items);
            public static Node Object(params Node[] members) => new Node(Kinds.Object, members);
            public static Node Abstract(Node type, Node value) => Object(Member("$t", type), Member("$v", value));
            public static Node Reference(int reference) => Object(Member("$r", Number(reference)));

            public readonly Kinds Kind;
            public readonly string Value;
            public readonly Node[] Children;

            public Node(Kinds kind, params Node[] children)
            {
                Kind = kind;
                Children = children;
                Value = default;
            }

            public Node(Kinds kind, string value)
            {
                Kind = kind;
                Value = value;
                Children = System.Array.Empty<Node>();
            }

            public override string ToString() =>
                Kind == Kinds.Member ? $"{Children[0]}, {Children[1]}" :
                string.IsNullOrEmpty(Value) ? $"{Kind}({Children.Length})" : $"{Kind}: {Value}";
        }

        public readonly struct ConvertToContext
        {
            static readonly Type _runtimeType = typeof(Type).GetType();

            public readonly Dictionary<object, int> References;
            public readonly Container Container;

            public ConvertToContext(Dictionary<object, int> references, Container container = null)
            {
                References = references;
                Container = container ?? new Container();
            }

            public Result<Node> To<T>(in T value) => To(value, typeof(T));

            public Result<Node> To(object value, Type type)
            {
                if (value is Node node) return node;
                if (value is null) return Node.Null;
                if (References.TryGetValue(value, out var reference)) return Node.Reference(reference);
                var dynamic = value.GetType();
                return dynamic == type ? Concrete(value, dynamic) : Abstract(value, dynamic);
            }

            Result<Node> Abstract(object value, Type dynamic) =>
                Result.And(To(dynamic, _runtimeType), Concrete(value, dynamic))
                    .Map(pair => Node.Abstract(pair.Item1, pair.Item2));

            Result<Node> Concrete(object value, Type dynamic)
            {
                var data = TypeUtility.GetData(dynamic);
                switch (data.Code)
                {
                    case TypeCode.Char: return Node.Number((char)value);
                    case TypeCode.Byte: return Node.Number((byte)value);
                    case TypeCode.SByte: return Node.Number((sbyte)value);
                    case TypeCode.Int16: return Node.Number((short)value);
                    case TypeCode.Int32: return Node.Number((int)value);
                    case TypeCode.Int64: return Node.Number((long)value);
                    case TypeCode.UInt16: return Node.Number((ushort)value);
                    case TypeCode.UInt32: return Node.Number((uint)value);
                    case TypeCode.UInt64: return Node.Number((ulong)value);
                    case TypeCode.Single: return Node.Number((float)value);
                    case TypeCode.Double: return Node.Number((double)value);
                    case TypeCode.Decimal: return Node.Number((decimal)value);
                    case TypeCode.Boolean: return Node.Boolean((bool)value);
                    case TypeCode.String: return Node.String((string)value);
                }

                References[value] = References.Count;
                if (Container.TryGet<IConverter>(dynamic, out var converter)) return converter.To(value, this);

                switch (value)
                {
                    case DateTime date:
                        return Node.String(date.ToUniversalTime().ToString());
                    case IConvertible convertible:
                        return Result.Try(convertible, state => Node.Number(state.ToDecimal(null)));
                    case Array array:
                        {
                            var items = new Node[array.Length];
                            var element = data.Element;
                            for (int i = 0; i < items.Length; i++)
                            {
                                var result = To(array.GetValue(i), element);
                                if (result.TryValue(out var item)) items[i] = item;
                                else return result;
                            }
                            return Node.Array(items);
                        }
                    default:
                        var fields = data.InstanceFields;
                        var members = new Node[fields.Length];
                        for (int i = 0; i < fields.Length; i++)
                        {
                            var field = fields[i];
                            var result = To(field.GetValue(value), field.FieldType);
                            if (result.TryValue(out var member)) members[i] = Node.Member(field.Name, member);
                            else return result;
                        }
                        return Node.Object(members);
                }
            }
        }

        public readonly struct ConvertFromContext
        {
            static readonly Type _runtimeType = typeof(Type).GetType();

            public readonly List<object> References;
            public readonly Container Container;

            public ConvertFromContext(List<object> references, Container container = null)
            {
                References = references;
                Container = container ?? new Container();
            }

            public Result<T> From<T>(Node node)
            {
                var result = From(node, typeof(T));
                if (result.TryValue(out var value)) return (T)value;
                return result.AsFailure();
            }

            public Result<object> From(Node node, Type type)
            {
                if (type == typeof(Node)) return node;
                switch (node.Kind)
                {
                    case Kinds.Null: return null;
                    case Kinds.Object:
                        if (node.Children.Length == 1)
                        {
                            var member = node.Children[0];
                            var key = member.Children[0];
                            var value = member.Children[1];
                            if (key.Value == $"r") return Result.Try(
                                (value.Value, References),
                                state => state.References[int.Parse(state.Value)]);
                        }
                        else if (node.Children.Length == 2)
                        {
                            var typeMember = node.Children[0];
                            var typeKey = typeMember.Children[0];
                            var typeValue = typeMember.Children[1];
                            var valueMember = node.Children[1];
                            var valueKey = valueMember.Children[0];
                            var valueValue = valueMember.Children[1];
                            if (typeKey.Value == "$t" && valueKey.Value == "$v")
                                return From(typeValue, _runtimeType).Cast<Type>().Bind(
                                    (@this: this, valueValue),
                                    (concrete, state) => state.@this.From(state.valueValue, concrete));
                        }
                        break;
                }

                if (Container.TryGet<IConverter>(type, out var converter)) return converter.From(node, this);

                switch (node.Kind)
                {
                    case Kinds.Null: return null;
                    case Kinds.Number:
                        return Result.Try(
                            (node.Value, type),
                            state => System.Convert.ChangeType(decimal.Parse(state.Value), state.type));
                    case Kinds.String:
                        return Result.Try(
                            (node.Value, type),
                            state => System.Convert.ChangeType(state.Value, state.type));
                    case Kinds.Boolean:
                        return Result.Try(node.Value, state => bool.Parse(state));
                }

                switch (node.Kind)
                {
                    case Kinds.Array:
                        var element = type.GetElementType();
                        var array = Array.CreateInstance(element, node.Children.Length);
                        References.Add(array);
                        for (int i = 0; i < node.Children.Length; i++)
                        {
                            var result = From(node.Children[i], element);
                            if (result.TryValue(out var item)) array.SetValue(item, i);
                            else return result;
                        }
                        return array;
                    case Kinds.Object:
                        var @object = Activator.CreateInstance(type, true);
                        References.Add(@object);
                        var fields = type.InstanceFields();
                        var data = TypeUtility.GetData(type);
                        foreach (var child in node.Children)
                        {
                            if (child.Kind == Kinds.Member &&
                                child.Children[0].Value is string key &&
                                data.Fields.TryGetValue(key, out var field))
                            {
                                var result = From(child.Children[1], field.FieldType);
                                if (result.TryValue(out var value)) field.SetValue(@object, value);
                                else return result;
                            }
                        }
                        return @object;
                }

                return Result.Failure($"Failed to deserialize node of type '{node.Kind}'.");
            }
        }

        [Implementation(typeof(Assembly), typeof(AssemblyConverter))]
        [Implementation(typeof(Type), typeof(TypeConverter))]
        public interface IConverter : ITrait
        {
            Result<Node> To(object value, in ConvertToContext context);
            Result<object> From(Node node, in ConvertFromContext context);
        }

        public abstract class Converter<T> : IConverter
        {
            public abstract Result<Node> To(in T value, in ConvertToContext context);
            public abstract Result<T> From(Node node, in ConvertFromContext context);
            Result<Node> IConverter.To(object value, in ConvertToContext context) =>
                Result.Cast<T>(value).Bind((@this: this, context), (casted, state) => state.@this.To(casted, state.context));
            Result<object> IConverter.From(Node node, in ConvertFromContext context) => From(node, context).Box();
        }

        public sealed class AssemblyConverter : Converter<Assembly>
        {
            public override Result<Node> To(in Assembly value, in ConvertToContext context) =>
                Node.String(value.GetName().Name);
            public override Result<Assembly> From(Node node, in ConvertFromContext context) =>
                Result.Try(node.Value, state => Assembly.Load(state));
        }

        public sealed class TypeConverter : Converter<Type>
        {
            enum Kinds { Type = 1, Array = 2, Pointer = 3, Generic = 4 }

            static readonly Type _runtimeAssembly = typeof(Type).Assembly.GetType();
            static readonly Type _runtimeType = typeof(Type).GetType();

            public override Result<Node> To(in Type value, in ConvertToContext context)
            {
                if (_indices.TryGetValue(value, out var index))
                    return Node.Number(index);
                else if (value.IsArray)
                    return context.To(value.GetElementType(), _runtimeType)
                        .Map(value.GetArrayRank(), (type, state) => Node.Array(Kinds.Array, state, type));
                else if (value.IsPointer)
                    return context.To(value.GetElementType(), _runtimeType)
                        .Map(type => Node.Array(Kinds.Pointer, type));
                else if (value.IsConstructedGenericType)
                {
                    var definition = value.GetGenericTypeDefinition();
                    var definitionResult = context.To(definition, _runtimeType);
                    if (definitionResult.TryValue(out var type))
                    {
                        var arguments = value.GetGenericArguments();
                        var items = new Node[arguments.Length + 2];
                        items[0] = Kinds.Generic;
                        items[1] = type;
                        for (int i = 0; i < arguments.Length; i++)
                        {
                            var argument = arguments[i];
                            var argumentResult = context.To(argument, _runtimeType);
                            if (argumentResult.TryValue(out var item)) items[i + 2] = item;
                            else return argumentResult;
                        }
                        return Node.Array(items);
                    }
                    return definitionResult;
                }
                else if (TypeUtility.TryGetGuid(value, out var guid))
                    return Node.String(guid.ToString());
                else
                    return context.To(value.Assembly, _runtimeAssembly)
                        .Map(value.FullName, (assembly, state) => Node.Array(Kinds.Type, assembly, state));
            }

            public override Result<Type> From(Node node, in ConvertFromContext context)
            {
                switch (node.Kind)
                {
                    case Json.Kinds.Number:
                        return Result.Try(node.Value, state => _types[int.Parse(state)]);
                    case Json.Kinds.String:
                        if (Guid.TryParse(node.Value, out var guid) && TypeUtility.TryGetType(guid, out var value))
                            return value;
                        return Result.Failure($"Failed to find type with guid '{node.Value}'.");
                    case Json.Kinds.Array:
                        var kindResult = Result.Try(node.Children[0].Value, state => Enum.Parse(typeof(Kinds), state)).Cast<Kinds>();
                        if (kindResult.TryValue(out var kind))
                        {
                            switch (kind)
                            {
                                case Kinds.Type:
                                    return context.From(node.Children[1], _runtimeAssembly)
                                        .Cast<Assembly>()
                                        .Map(node.Children[2].Value, (assembly, state) => assembly.GetType(state));
                                case Kinds.Array:
                                    return Result.And(
                                        Result.Try(node.Children[1].Value, state => int.Parse(state)),
                                        context.From(node.Children[2], _runtimeType).Cast<Type>())
                                    .Map(pair => pair.Item1 > 1 ? pair.Item2.MakeArrayType(pair.Item1) : pair.Item2.MakeArrayType());
                                case Kinds.Pointer:
                                    return context.From(node.Children[1], _runtimeType).Cast<Type>().Map(type => type.MakePointerType());
                                case Kinds.Generic:
                                    var definitionResult = context.From(node.Children[1], _runtimeType).Cast<Type>();
                                    if (definitionResult.TryValue(out var definition))
                                    {
                                        var arguments = new Type[node.Children.Length - 2];
                                        for (int i = 0; i < arguments.Length; i++)
                                        {
                                            var argumentResult = context.From(node.Children[i + 2], _runtimeType).Cast<Type>();
                                            if (argumentResult.TryValue(out var argument)) arguments[i] = argument;
                                            else return argumentResult;
                                        }
                                        return definition.MakeGenericType(arguments);
                                    }
                                    return definitionResult;
                            }
                        }
                        return kindResult.AsFailure();
                }
                return default;
            }
        }

        [Preserve]
        readonly struct Members
        {
            [Preserve]
            public readonly object Field;
            [Preserve]
            public object Property { get; }
            [Preserve]
            public void Method() { }
            [Preserve]
            public Members(object field, object property) { Field = field; Property = property; }
        }

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
            typeof(Stack<>), typeof(Queue<>), typeof(HashSet<>), typeof(Dictionary<,>),
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

        public static Result<Node> Parse(string text)
        {
            char Next(ref int index)
            {
                while (index < text.Length)
                {
                    var character = text[index++];
                    if (char.IsWhiteSpace(character)) continue;
                    return character;
                }
                return '\0';
            }

            char At(int index) => index < text.Length ? text[index] : '\0';

            FormatException Except(string expected, int index) =>
                new FormatException($"Expected '{expected}' at index '{index}'.");

            Node? Value(ref int index)
            {
                var start = index;
                switch (Next(ref index))
                {
                    case 'n':
                        if (At(index++) == 'u' && At(index++) == 'l' && At(index++) == 'l')
                            return Node.Null;
                        throw Except(Node.Null.Value, start);
                    case 't':
                        if (At(index++) == 'r' && At(index++) == 'u' && At(index++) == 'e')
                            return Node.True;
                        throw Except(Node.True.Value, start);
                    case 'f':
                        if (At(index++) == 'a' && At(index++) == 'l' && At(index++) == 's' && At(index++) == 'e')
                            return Node.False;
                        throw Except(Node.False.Value, start);
                    case '-':
                    case '+':
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        {
                            // NOTE: integer part
                            while (index < text.Length && char.IsDigit(text[index])) index++;

                            // NOTE: fraction part
                            if (At(index) == '.')
                            {
                                index++;
                                while (index < text.Length && char.IsDigit(text[index])) index++;
                            }

                            // NOTE: exponent part
                            var exponent = At(index);
                            if (exponent == 'e' || exponent == 'E')
                            {
                                index++;
                                if (index < text.Length)
                                {
                                    var character = text[index];
                                    if (character != '-' && character != '+' && !char.IsDigit(character))
                                        throw Except("valid exponent", index);
                                    index++;
                                }
                                while (index < text.Length && char.IsDigit(text[index])) index++;
                            }

                            return new Node(Kinds.Number, text.Substring(start, index - start));
                        }
                    case '"':
                        {
                            start++;
                            while (index < text.Length && text[index] != '"') index++;
                            if (At(index) == '"') return text.Substring(start, index++ - start);
                            throw Except("end of string", index);
                        }
                    case '{':
                        {
                            var members = new List<Node>();
                            while (Value(ref index) is Node key)
                            {
                                if (Next(ref index) == ':' && Value(ref index) is Node value) members.Add(Node.Member(key, value));
                                else throw Except("value", index);

                                if (Next(ref index) == ',') continue;
                                break;
                            }
                            if (At(index - 1) == '}') return Node.Object(members.ToArray());
                            throw Except("end of object", index);
                        }
                    case '[':
                        {
                            var items = new List<Node>();
                            while (Value(ref index) is Node item)
                            {
                                items.Add(item);
                                if (Next(ref index) == ',') continue;
                                break;
                            }
                            if (At(index - 1) == ']') return Node.Array(items.ToArray());
                            throw Except("end of array", index);
                        }
                    case '}':
                    case ']':
                    case '\0': return default;
                    default: throw Except("valid character", index);
                }
            }

            var current = 0;
            return Result.Try(() =>
                Value(ref current) is Node root && Next(ref current) == '\0' ?
                root : throw Except("end of json", current));
        }

        public static string Serialize<T>(in T value, Format format = Format.Compact) => Serialize(Convert(value, typeof(T)), format);

        public static string Serialize(Node node, Format format = Format.Compact)
        {
            var builder = new StringBuilder(64);
            switch (format)
            {
                case Format.Compact: SerializeCompact(node, builder); break;
                case Format.Indented: SerializeIndented(node, builder, 0); break;
                default: throw new NotSupportedException($"Invalid format '{format}'.");
            }
            return builder.ToString();
        }

        public static Result<Node> Convert(object value, Type type, params object[] references)
        {
            var dictionary = new Dictionary<object, int>(_types.Length + references.Length);
            for (int i = 0; i < references.Length; i++) dictionary[references[i]] = dictionary.Count;
            return new ConvertToContext(dictionary).To(value, type);
        }

        public static Result<T> Deserialize<T>(string json) => Parse(json).Map(node => Deserialize<T>(node));
        public static Result<object> Deserialize(string json, Type type) => Parse(json).Map(node => Deserialize(node, type));
        public static T Deserialize<T>(Node node) => Deserialize(node, typeof(T)) is T value ? value : default;
        public static object Deserialize(Node node, Type type, params object[] references)
        {
            var list = new List<object>(references.Length);
            for (int i = 0; i < references.Length; i++) list.Add(references[i]);
            return new ConvertFromContext(list).From(node, type);
        }

        static void SerializeCompact(Node node, StringBuilder builder)
        {
            switch (node.Kind)
            {
                case Kinds.Null:
                case Kinds.Boolean:
                case Kinds.Number: builder.Append(node.Value); break;
                case Kinds.String:
                    builder.Append('"');
                    builder.Append(node.Value);
                    builder.Append('"');
                    break;
                case Kinds.Array:
                    builder.Append('[');
                    for (int i = 0; i < node.Children.Length; i++)
                    {
                        if (i > 0) builder.Append(',');
                        SerializeCompact(node.Children[i], builder);
                    }
                    builder.Append(']');
                    break;
                case Kinds.Object:
                    builder.Append('{');
                    for (int i = 0; i < node.Children.Length; i++)
                    {
                        if (i > 0) builder.Append(',');
                        SerializeCompact(node.Children[i], builder);
                    }
                    builder.Append('}');
                    break;
                case Kinds.Member:
                    SerializeCompact(node.Children[0], builder);
                    builder.Append(':');
                    SerializeCompact(node.Children[1], builder);
                    break;
            }
        }

        static void SerializeIndented(Node node, StringBuilder builder, int indent)
        {
            switch (node.Kind)
            {
                case Kinds.Null:
                case Kinds.Boolean:
                case Kinds.Number: builder.Append(node.Value); break;
                case Kinds.String:
                    builder.Append('"');
                    builder.Append(node.Value);
                    builder.Append('"');
                    break;
                case Kinds.Array:
                    builder.Append('[');
                    builder.AppendLine();
                    indent++;
                    for (int i = 0; i < node.Children.Length; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append(',');
                            builder.AppendLine();
                        }
                        Indent(indent, builder);
                        SerializeIndented(node.Children[i], builder, indent);
                    }
                    indent--;
                    builder.AppendLine();
                    Indent(indent, builder);
                    builder.Append(']');
                    break;
                case Kinds.Object:
                    builder.Append('{');
                    builder.AppendLine();
                    indent++;
                    for (int i = 0; i < node.Children.Length; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append(',');
                            builder.AppendLine();
                        }
                        Indent(indent, builder);
                        SerializeIndented(node.Children[i], builder, indent);
                    }
                    indent--;
                    builder.AppendLine();
                    Indent(indent, builder);
                    builder.Append('}');
                    break;
                case Kinds.Member:
                    SerializeIndented(node.Children[0], builder, indent);
                    builder.Append(" : ");
                    SerializeIndented(node.Children[1], builder, indent);
                    break;
            }
        }

        static void Indent(int indent, StringBuilder builder) => builder.Append(new string(' ', indent * 2));
    }
}