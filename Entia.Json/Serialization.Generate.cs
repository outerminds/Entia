using System;
using System.Runtime.CompilerServices;
using System.Text;
using Entia.Core;

namespace Entia.Json
{
    public enum GenerateFormat { Compact, Indented }

    public static partial class Serialization
    {
        public static string Serialize<T>(in T instance, ConvertOptions options = ConvertOptions.All, GenerateFormat format = GenerateFormat.Compact, Container container = null, params object[] references) =>
            Generate(Convert(instance, options, container, references), format);
        public static string Serialize(object instance, Type type, ConvertOptions options = ConvertOptions.All, GenerateFormat format = GenerateFormat.Compact, Container container = null, params object[] references) =>
            Generate(Convert(instance, type, options, container, references), format);

        public static string Generate(Node node, GenerateFormat format = GenerateFormat.Compact)
        {
            var builder = new StringBuilder(1024);
            switch (format)
            {
                case GenerateFormat.Compact: GenerateCompact(node, builder); break;
                case GenerateFormat.Indented: GenerateIndented(node, builder, 0); break;
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

        static char ToHex(int value)
        {
            switch (value)
            {
                case 0: return '0';
                case 1: return '1';
                case 2: return '2';
                case 3: return '3';
                case 4: return '4';
                case 5: return '5';
                case 6: return '6';
                case 7: return '7';
                case 8: return '8';
                case 9: return '9';
                case 10: return 'A';
                case 11: return 'B';
                case 12: return 'C';
                case 13: return 'D';
                case 14: return 'E';
                case 15: return 'F';
                default: return '\0';
            }
        }
    }
}