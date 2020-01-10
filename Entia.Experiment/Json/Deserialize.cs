using System;
using System.Collections.Generic;
using Entia.Core;

namespace Entia.Experiment.Json
{
    public static partial class Serialization
    {
        public static Result<T> Deserialize<T>(string json, Container container = null, params object[] references)
        {
            var result = Parse(json);
            if (result.TryValue(out var node)) return Instantiate<T>(node, container, references);
            return result.AsFailure();
        }

        public static Result<object> Deserialize(string json, Type type, Container container = null, params object[] references)
        {
            var result = Parse(json);
            if (result.TryValue(out var node)) return Instantiate(node, type, container, references);
            return result.AsFailure();
        }

        public static unsafe Result<Node> Parse(string text)
        {
            var count = text.Length;
            if (count == 0) return Node.Null;

            var index = 0;
            var nodes = new Stack<Node>(32);
            var counts = new Stack<int>(8);
            fixed (char* pointer = text)
            {
                while (index < count)
                {
                    switch (pointer[index++])
                    {
                        case 'n':
                            if (index + 3 < count && pointer[index++] == 'u' && pointer[index++] == 'l' && pointer[index++] == 'l')
                                nodes.Push(Node.Null);
                            else
                                return Result.Failure($"Expected 'null' at index '{index}'.");
                            break;
                        case 't':
                            if (index + 3 < count && pointer[index++] == 'r' && pointer[index++] == 'u' && pointer[index++] == 'e')
                                nodes.Push(Node.True);
                            else
                                return Result.Failure($"Expected 'true' at index '{index}'.");
                            break;
                        case 'f':
                            if (index + 4 < count && pointer[index++] == 'a' && pointer[index++] == 'l' && pointer[index++] == 's' && pointer[index++] == 'e')
                                nodes.Push(Node.False);
                            else
                                return Result.Failure($"Expected 'false' at index '{index}'.");
                            break;
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
                                var start = index - 1;
                                // NOTE: integer part
                                while (index < count && char.IsDigit(pointer[index])) index++;

                                // NOTE: fraction part
                                if (index < count && pointer[index] == '.')
                                {
                                    index++;
                                    while (index < count && char.IsDigit(pointer[index])) index++;
                                }

                                // NOTE: exponent part
                                if (index < count && (pointer[index] == 'e' || pointer[index] == 'E'))
                                {
                                    index++;
                                    if (index < count && (pointer[index] == '-' || pointer[index] == '+')) index++;
                                    while (index < count && char.IsDigit(pointer[index])) index++;
                                }

                                nodes.Push(new Node(Node.Kinds.Number, new string(pointer, start, index - start)));
                                break;
                            }
                        case '"':
                            {
                                var start = index;
                                while (index < count)
                                {
                                    if (pointer[index++] == '"')
                                    {
                                        nodes.Push(new Node(Node.Kinds.String, new string(pointer, start, index - 1 - start)));
                                        break;
                                    }
                                }
                                break;
                            }
                        case '{':
                        case '[': counts.Push(nodes.Count); break;
                        case '}':
                            var members = new Node[(nodes.Count - counts.Pop()) / 2];
                            for (var i = members.Length - 1; i >= 0; i--)
                            {
                                var value = nodes.Pop();
                                var key = nodes.Pop();
                                if (key.Kind == Node.Kinds.String) members[i] = Node.Member(key, value);
                                else return Result.Failure("Expected key to be of type string.");
                            }
                            nodes.Push(Node.Object(members));
                            break;
                        case ']':
                            var items = new Node[nodes.Count - counts.Pop()];
                            for (var i = items.Length - 1; i >= 0; i--) items[i] = nodes.Pop();
                            nodes.Push(Node.Array(items));
                            break;
                    }
                }
            }

            return nodes.Pop();
        }
    }
}
