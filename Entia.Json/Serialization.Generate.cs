using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Entia.Core;

namespace Entia.Json
{
    public enum Formats { Compact, Indented }

    public static partial class Serialization
    {
        public static string Serialize<T>(in T instance, Features features = Features.None, Formats format = Formats.Compact, Container container = null)
        {
            var context = ToContext(features, container);
            var node = context.Convert(instance);
            return Generate(node, format, new Dictionary<uint, int>(), context.References);
        }

        public static string Serialize(object instance, Type type, Features features = Features.None, Formats format = Formats.Compact, Container container = null)
        {
            var context = ToContext(features, container);
            var node = context.Convert(instance, type);
            return Generate(node, format, new Dictionary<uint, int>(), context.References);
        }

        public static string Generate(Node node, Formats format = Formats.Compact) =>
            Generate(node, format, new Dictionary<uint, int>(), new Dictionary<object, uint>());

        static string Generate(Node node, Formats format, Dictionary<uint, int> identifiers, Dictionary<object, uint> references)
        {
            node = Wrap(node, identifiers, references);
            var builder = new StringBuilder(1024);
            switch (format)
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
                case Node.Kinds.Number: builder.Append(node.Value); break;
                case Node.Kinds.String:
                    var value = (string)node.Value;
                    if (value.Length == 0) builder.Append(@"""""");
                    else
                    {
                        builder.Append(@"""");
                        if (node.HasPlain()) builder.Append(value);
                        else builder.AppendEscaped(value);
                        builder.Append(@"""");
                    }
                    break;
                case Node.Kinds.Array:
                    if (node.Children.Length == 0) builder.Append("[]");
                    else
                    {
                        builder.Append('[');
                        for (int i = 0; i < node.Children.Length; i++)
                        {
                            if (i > 0) builder.Append(',');
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
                        for (int i = 0; i < node.Children.Length; i += 2)
                        {
                            if (i > 0) builder.Append(',');
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
                case Node.Kinds.Number: builder.Append(node.Value); break;
                case Node.Kinds.String:
                    var value = (string)node.Value;
                    if (value.Length == 0) builder.Append(@"""""");
                    else
                    {
                        builder.Append(@"""");
                        if (node.HasPlain()) builder.Append(value);
                        else builder.AppendEscaped(value);
                        builder.Append(@"""");
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

        static Node Wrap(Node node, Dictionary<uint, int> identifiers, Dictionary<object, uint> references)
        {
            switch (node.Kind)
            {
                case Node.Kinds.Reference:
                    var reference = node.AsReference();
                    // NOTE: no need to visit children
                    return Node.Object("$r",
                        identifiers.TryGetValue(reference, out var index) ?
                        index : identifiers[reference] = identifiers.Count);
                case Node.Kinds.Abstract:
                    if (node.TryAbstract(out var type, out var value))
                        node = Node.Object("$a", JsonUtility.TypeToNode(type, references), "$v", value);
                    break;
                case Node.Kinds.Type:
                    node = Node.Object("$t", JsonUtility.TypeToNode(node.AsType(), references));
                    break;
            }

            var children = default(Node[]);
            for (int i = 0; i < node.Children.Length; i++)
            {
                var child = node.Children[i];
                var wrapped = Wrap(child, identifiers, references);
                if (child == wrapped) continue;
                children ??= (Node[])node.Children.Clone();
                children[i] = wrapped;
            }
            if (children != null) node = node.With(children);

            if (identifiers.TryGetValue(node.Identifier, out var identifier))
                return Node.Object("$i", identifier, "$v", node);

            return node;
        }
    }
}