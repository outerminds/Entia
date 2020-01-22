using System;
using System.Runtime.CompilerServices;
using Entia.Core;

namespace Entia.Json
{
    public static partial class Serialization
    {
        const char _a = 'a', _b = 'b', _c = 'c', _d = 'd', _e = 'e', _f = 'f';
        const char _A = 'A', _B = 'B', _C = 'C', _D = 'D', _E = 'E', _F = 'F';
        const char _l = 'l', _n = 'n', _r = 'r', _s = 's', _t = 't', _u = 'u';
        const char _0 = '0', _1 = '1', _2 = '2', _3 = '3', _4 = '4', _5 = '5', _6 = '6', _7 = '7', _8 = '8', _9 = '9';
        const char _plus = '+', _minus = '-', _comma = ',', _dot = '.', _colon = ':', _quote = '"', _slash = '\\';
        const char _openCurly = '{', _closeCurly = '}', _openSquare = '[', _closeSquare = ']';
        const char _tab = '\t', _space = ' ', _line = '\n', _return = '\r';

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
            var index = 0;
            var count = text.Length;
            var nodes = (items: new Node[64], count: 0);
            var counts = (items: new int[8], count: 0);
            // var builder = (items: new char[64], count: 0);
            fixed (char* pointer = text)
            {
                while (index < count)
                {
                    switch (pointer[index++])
                    {
                        case _n:
                            if (index + 3 <= count && pointer[index++] == _u && pointer[index++] == _l && pointer[index++] == _l)
                                nodes.Push(Node.Null);
                            else
                                return Result.Failure($"Expected 'null' at index '{index - 1}'.");
                            break;
                        case _t:
                            if (index + 3 <= count && pointer[index++] == _r && pointer[index++] == _u && pointer[index++] == _e)
                                nodes.Push(Node.True);
                            else
                                return Result.Failure($"Expected 'true' at index '{index - 1}'.");
                            break;
                        case _f:
                            if (index + 4 <= count && pointer[index++] == _a && pointer[index++] == _l && pointer[index++] == _s && pointer[index++] == _e)
                                nodes.Push(Node.False);
                            else
                                return Result.Failure($"Expected 'false' at index '{index - 1}'.");
                            break;
                        case _plus:
                        case _minus:
                        case _0:
                        case _1:
                        case _2:
                        case _3:
                        case _4:
                        case _5:
                        case _6:
                        case _7:
                        case _8:
                        case _9:
                            {
                                var start = index - 1;
                                // NOTE: integer part
                                while (index < count && IsDigit(pointer[index])) index++;

                                // NOTE: fraction part
                                if (index < count && pointer[index] == _dot)
                                {
                                    index++;
                                    while (index < count && IsDigit(pointer[index])) index++;
                                }

                                // NOTE: exponent part
                                if (index < count && (pointer[index] == _e || pointer[index] == _E))
                                {
                                    index++;
                                    if (index < count && (pointer[index] == _minus || pointer[index] == _plus)) index++;
                                    while (index < count && IsDigit(pointer[index])) index++;
                                }

                                nodes.Push(new Node(Node.Kinds.Number, new string(pointer, start, index - start)));
                                break;
                            }
                        case _quote:
                            {
                                var start = index;
                                while (index < count)
                                {
                                    var current = pointer[index++];
                                    if (current == _slash) index++;
                                    // TODO: unescape characters?
                                    // {
                                    //     // TODO: handle "\u0000"
                                    //     builder.Push(pointer[index++]);
                                    // }
                                    else if (current == _quote)
                                    {
                                        nodes.Push(new Node(Node.Kinds.String, new string(pointer, start, index - 1 - start)));
                                        // nodes.Push(new Node(Node.Kinds.String, new string(builder.items, 0, builder.count)));
                                        // builder.count = 0;
                                        break;
                                    }
                                    // else builder.Push(current);
                                }
                                break;
                            }
                        case _openCurly:
                        case _openSquare: counts.Push(nodes.count); break;
                        case _closeCurly:
                            if (counts.TryPop(out var memberCount))
                            {
                                var members = new Node[nodes.count - memberCount];
                                for (var i = members.Length - 1; i >= 0; i--) members[i] = nodes.Pop();
                                nodes.Push(Node.Object(members));
                                break;
                            }
                            return Result.Failure($"Expected balanced curly bracket at index '{index - 1}'.");
                        case _closeSquare:
                            if (counts.TryPop(out var itemCount))
                            {
                                var items = new Node[nodes.count - itemCount];
                                for (var i = items.Length - 1; i >= 0; i--) items[i] = nodes.Pop();
                                nodes.Push(Node.Array(items));
                                break;
                            }
                            return Result.Failure($"Expected balanced square bracket at index '{index - 1}'.");
                        case _space: case _tab: case _line: case _return: case _comma: case _colon: break;
                        default: return Result.Failure($"Expected character '{pointer[index - 1]}' at index '{index - 1}' to be valid.");
                    }
                }
            }

            if (nodes.count == 0) return Result.Failure("Failed to parse json.");
            return nodes.Pop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte ToHex(char character)
        {
            switch (character)
            {
                case _0:
                case _1:
                case _2:
                case _3:
                case _4:
                case _5:
                case _6:
                case _7:
                case _8:
                case _9: return (byte)(character - _0);
                case _a:
                case _b:
                case _c:
                case _d:
                case _e:
                case _f: return (byte)(character - _a + 10);
                case _A:
                case _B:
                case _C:
                case _D:
                case _E:
                case _F: return (byte)(character - _A + 10);
                default: return 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsDigit(char character) => character >= '0' && character <= '9';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Push<T>(ref this (T[] items, int count) array, T item)
        {
            var index = array.count++;
            ArrayUtility.Ensure(ref array.items, array.count);
            array.items[index] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static T Pop<T>(ref this (T[] items, int count) array) => array.items[--array.count];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryPop<T>(ref this (T[] items, int count) array, out T item)
        {
            if (array.count > 0)
            {
                item = array.items[--array.count];
                return true;
            }
            item = default;
            return false;
        }
    }
}