using System;
using System.Collections.Generic;

namespace Entia.Modules.Serialization
{
    public static class Json
    {
        public enum Kinds { Null, True, False, Number, String, Object, Pair, Array }

        public readonly struct Node
        {
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
                Children = Array.Empty<Node>();
            }
        }

        public static Node Parse(string text)
        {
            char Next(ref int index)
            {
                if (index >= text.Length) return '\0';
                var character = text[index++];
                if (char.IsWhiteSpace(character)) return Next(ref index);
                return character;
            }

            char Current(int index) => index < text.Length ? text[index] : '\0';

            bool Expect(char character, int index) =>
                Current(index) == character ? true : throw new FormatException($"Expected '{character}'");

            Node? Value(ref int index)
            {
                switch (Next(ref index))
                {
                    case 'n':
                        if (Expect('u', index++) && Expect('l', index++) && Expect('l', index++))
                            return new Node(Kinds.Null, "null");
                        throw new FormatException("Expected 'null'.");
                    case 't':
                        if (Expect('r', index++) && Expect('u', index++) && Expect('e', index++))
                            return new Node(Kinds.True, "true");
                        throw new FormatException("Expected 'true'.");
                    case 'f':
                        if (Expect('a', index++) && Expect('l', index++) && Expect('s', index++) && Expect('e', index++))
                            return new Node(Kinds.False, "false");
                        throw new FormatException("Expected 'false'.");
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
                            var start = index - 1;
                            var count = 1;
                            while (index < text.Length && char.IsDigit(text[index])) { index++; count++; }
                            if (Current(index) == '.')
                            {
                                index++;
                                count++;
                                while (index < text.Length && char.IsDigit(text[index])) { index++; count++; }
                            }
                            return new Node(Kinds.Number, text.Substring(start, count));
                        }
                    case '"':
                        {
                            var start = index;
                            var count = 0;
                            while (index < text.Length && text[index++] != '"') count++;
                            return new Node(Kinds.String, text.Substring(start, count));
                        }
                    case '{':
                        {
                            var children = new List<Node>();
                            while (Value(ref index) is Node key)
                            {
                                if (Next(ref index) == ':' && Value(ref index) is Node value) children.Add(new Node(Kinds.Pair, key, value));
                                else throw new FormatException("Expected value.");

                                if (Next(ref index) == ',') continue;
                                break;
                            }
                            if (Expect('}', index - 1)) return new Node(Kinds.Object, children.ToArray());
                            throw new FormatException($"Expected end of object.");
                        }
                    case '[':
                        {
                            var children = new List<Node>();
                            while (Value(ref index) is Node child)
                            {
                                children.Add(child);
                                if (Next(ref index) == ',') continue;
                                break;
                            }
                            if (Expect(']', index - 1)) return new Node(Kinds.Array, children.ToArray());
                            throw new FormatException($"Expected end of array.");
                        }
                    case '}':
                    case ']':
                    case '\0': return default;
                    default: throw new FormatException("Expected valid character.");
                }
            }

            var current = 0;
            return
                Value(ref current) is Node root && Expect('\0', current) ?
                root : throw new FormatException("Expected valid text.");
        }
    }
}