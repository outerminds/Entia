using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Entia.Json.Converters;

namespace Entia.Json
{
    public static partial class Serialization
    {
        static string Generate(Node node, in ToContext context)
        {
            Wrap(ref node, context);
            var builder = new StringBuilder(1024);
            switch (context.Settings.Format)
            {
                case Formats.Compact: GenerateCompact(node, builder); break;
                case Formats.Indented: GenerateIndented(node, builder, 0); break;
            }
            return builder.ToString();
        }

        static void GenerateCompact(Node node, StringBuilder builder)
        {
            switch (node.Kind)
            {
                case Node.Kinds.Null: builder.Append("null"); break;
                case Node.Kinds.Boolean: builder.Append((bool)node.Value ? "true" : "false"); break;
                case Node.Kinds.Number: builder.Append(NumberToString(node)); break;
                case Node.Kinds.String:
                    var value = (string)node.Value;
                    if (value.Length == 0) builder.Append(@"""""");
                    else
                    {
                        builder.Append('"');
                        if (node.HasPlain()) builder.Append(value);
                        else builder.AppendEscaped(value);
                        builder.Append('"');
                    }
                    break;
                case Node.Kinds.Array:
                    if (node.Children.Length == 0) builder.Append("[]");
                    else
                    {
                        builder.Append('[');
                        GenerateCompact(node.Children[0], builder);
                        for (int i = 1; i < node.Children.Length; i++)
                        {
                            builder.Append(',');
                            GenerateCompact(node.Children[i], builder);
                        }
                        builder.Append(']');
                    }
                    break;
                case Node.Kinds.Object:
                    if (node.Children.Length == 0) builder.Append("{}");
                    else
                    {
                        builder.Append('{');
                        GenerateCompact(node.Children[0], builder);
                        builder.Append(':');
                        GenerateCompact(node.Children[1], builder);
                        for (int i = 2; i < node.Children.Length; i += 2)
                        {
                            builder.Append(',');
                            GenerateCompact(node.Children[i], builder);
                            builder.Append(':');
                            GenerateCompact(node.Children[i + 1], builder);
                        }
                        builder.Append('}');
                    }
                    break;
            }
        }

        static void GenerateIndented(Node node, StringBuilder builder, int indent)
        {
            switch (node.Kind)
            {
                case Node.Kinds.Null: builder.Append("null"); break;
                case Node.Kinds.Boolean: builder.Append((bool)node.Value ? "true" : "false"); break;
                case Node.Kinds.Number: builder.Append(NumberToString(node)); break;
                case Node.Kinds.String:
                    var value = (string)node.Value;
                    if (value.Length == 0) builder.Append(@"""""");
                    else
                    {
                        builder.Append('"');
                        if (node.HasPlain()) builder.Append(value);
                        else builder.AppendEscaped(value);
                        builder.Append('"');
                    }
                    break;
                case Node.Kinds.Array:
                    if (node.Children.Length == 0) builder.Append("[]");
                    else
                    {
                        builder.Append('[');
                        builder.AppendLine();
                        indent++;
                        for (int i = 0; i < node.Children.Length; i++)
                        {
                            if (i > 0)
                            {
                                builder.Append(", ");
                                builder.AppendLine();
                            }
                            Indent(indent, builder);
                            GenerateIndented(node.Children[i], builder, indent);
                        }
                        indent--;
                        builder.AppendLine();
                        Indent(indent, builder);
                        builder.Append(']');
                    }
                    break;
                case Node.Kinds.Object:
                    if (node.Children.Length == 0) builder.Append("{}");
                    else
                    {
                        builder.Append('{');
                        builder.AppendLine();
                        indent++;
                        for (int i = 0; i < node.Children.Length; i += 2)
                        {
                            if (i > 0)
                            {
                                builder.Append(", ");
                                builder.AppendLine();
                            }
                            Indent(indent, builder);
                            GenerateIndented(node.Children[i], builder, indent);
                            builder.Append(": ");
                            GenerateIndented(node.Children[i + 1], builder, indent);
                        }
                        indent--;
                        builder.AppendLine();
                        Indent(indent, builder);
                        builder.Append('}');
                    }
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string NumberToString(Node node) =>
            node.HasInteger() ? ((long)node.Value).ToString(CultureInfo.InvariantCulture) :
            node.HasRational() ? ((double)node.Value).ToString(CultureInfo.InvariantCulture) :
            System.Convert.ToString(node.Value, CultureInfo.InvariantCulture);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Indent(int indent, StringBuilder builder) => builder.Append(new string(' ', indent * 2));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AppendEscaped(this StringBuilder builder, string value)
        {
            var index = 0;
            var count = value.Length;
            for (int i = 0; i < count; i++)
            {
                var character = value[i];
                switch (character)
                {
                    case _line:
                        builder.Append(value, index, i - index);
                        builder.Append(@"\n");
                        index = i + 1;
                        break;
                    case _return:
                        builder.Append(value, index, i - index);
                        builder.Append(@"\r");
                        index = i + 1;
                        break;
                    case _tab:
                        builder.Append(value, index, i - index);
                        builder.Append(@"\t");
                        index = i + 1;
                        break;
                    case _feed:
                        builder.Append(value, index, i - index);
                        builder.Append(@"\f");
                        index = i + 1;
                        break;
                    case _back:
                        builder.Append(value, index, i - index);
                        builder.Append(@"\b");
                        index = i + 1;
                        break;
                    case _backSlash:
                        builder.Append(value, index, i - index);
                        builder.Append(@"\\");
                        index = i + 1;
                        break;
                    // NOTE: it seems that the front slash character does not need to be escaped
                    // case _frontSlash:
                    //     builder.Append(value, index, i - index);
                    //     builder.Append(@"\/");
                    //     index = i + 1;
                    //     break;
                    case _quote:
                        builder.Append(value, index, i - index);
                        builder.Append(@"\""");
                        index = i + 1;
                        break;
                    default:
                        if (character <= byte.MaxValue) continue;
                        builder.Append(@"\u");
                        builder.Append(ToHex(character >> 12));
                        builder.Append(ToHex((character >> 8) & 0xF));
                        builder.Append(ToHex((character >> 4) & 0xF));
                        builder.Append(ToHex(character & 0xF));
                        index = i + 1;
                        break;
                }
            }
            builder.Append(value, index, count - index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static char ToHex(int value) => value switch
        {
            0 => '0',
            1 => '1',
            2 => '2',
            3 => '3',
            4 => '4',
            5 => '5',
            6 => '6',
            7 => '7',
            8 => '8',
            9 => '9',
            10 => 'A',
            11 => 'B',
            12 => 'C',
            13 => 'D',
            14 => 'E',
            15 => 'F',
            _ => '\0',
        };

        static void Wrap(ref Node node, in ToContext context)
        {
            if (context.Settings.Features.HasAll(Features.Abstract))
            {
                context.References.Clear();
                node = ConvertTypes(node, context);
            }
            if (context.Settings.Features.HasAll(Features.Reference))
            {
                var identifiers = new Dictionary<uint, int>();
                // NOTE: it cannot be assumed in what order references appear relative to their referenced node;
                // as such, all references must be identified before wrapping identified nodes
                node = WrapReferences(node, identifiers);
                var count = 0;
                node = WrapIdentified(node, ref count, identifiers);
            }

            static Node ConvertType(Type type, in ToContext context) =>
                context.Convert(type, JsonType.Instance, JsonType.Instance);

            static Node ConvertTypes(Node node, in ToContext context)
            {
                if (node.TryAbstract(out var type, out var value))
                    return Node.Object(
                        Node.DollarTString, ConvertType(type, context),
                        Node.DollarVString, ConvertTypes(value, context));
                else if (node.TryType(out type))
                    return Node.Object(Node.DollarTString, ConvertType(type, context));

                var children = default(Node[]);
                for (int i = 0; i < node.Children.Length; i++)
                {
                    var child = node.Children[i];
                    var wrapped = ConvertTypes(child, context);
                    if (ReferenceEquals(child, wrapped)) continue;
                    children ??= (Node[])node.Children.Clone();
                    children[i] = wrapped;
                }
                return children == null ? node : node.With(children);
            }

            static Node WrapReferences(Node node, Dictionary<uint, int> identifiers)
            {
                if (node.TryReference(out var identifier))
                {
                    // NOTE: no need to visit children
                    return Node.Object(Node.DollarRString,
                        identifiers.TryGetValue(identifier, out var index) ?
                        index : identifiers[identifier] = identifiers.Count);
                }

                var children = default(Node[]);
                for (int i = 0; i < node.Children.Length; i++)
                {
                    var child = node.Children[i];
                    var wrapped = WrapReferences(child, identifiers);
                    if (ReferenceEquals(child, wrapped)) continue;
                    children ??= (Node[])node.Children.Clone();
                    children[i] = wrapped;
                }
                return children == null ? node : node.With(children);
            }

            static Node WrapIdentified(Node node, ref int count, Dictionary<uint, int> identifiers)
            {
                // NOTE: return early if all identified nodes have been wrapped
                if (count == identifiers.Count) return node;

                var children = default(Node[]);
                for (int i = 0; i < node.Children.Length; i++)
                {
                    var child = node.Children[i];
                    var wrapped = WrapIdentified(child, ref count, identifiers);
                    if (ReferenceEquals(child, wrapped)) continue;
                    children ??= (Node[])node.Children.Clone();
                    children[i] = wrapped;
                }
                node = children == null ? node : node.With(children);

                if (identifiers.TryGetValue(node.Identifier, out var index))
                {
                    count++;
                    return Node.Object(Node.DollarIString, index, Node.DollarVString, node);
                }

                return node;
            }
        }
    }
}