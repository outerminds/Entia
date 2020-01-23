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
            var brackets = (items: new int[8], count: 0);
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
                                    else if (current == _quote)
                                    {
                                        nodes.Push(new Node(Node.Kinds.String, new string(pointer, start, index - 1 - start)));
                                        break;
                                    }
                                }
                                break;
                            }
                        case _openCurly:
                        case _openSquare: brackets.Push(nodes.count); break;
                        case _closeCurly:
                            if (brackets.TryPop(out var memberCount))
                            {
                                var members = new Node[nodes.count - memberCount];
                                for (var i = members.Length - 1; i >= 0; i--) members[i] = nodes.Pop();
                                nodes.Push(Node.Object(members));
                                break;
                            }
                            else
                                return Result.Failure($"Expected balanced curly bracket at index '{index - 1}'.");
                        case _closeSquare:
                            if (brackets.TryPop(out var itemCount))
                            {
                                var items = new Node[nodes.count - itemCount];
                                for (var i = items.Length - 1; i >= 0; i--) items[i] = nodes.Pop();
                                nodes.Push(Node.Array(items));
                                break;
                            }
                            else
                                return Result.Failure($"Expected balanced square bracket at index '{index - 1}'.");
                        case _space: case _tab: case _line: case _return: case _comma: case _colon: break;
                        default: return Result.Failure($"Expected character '{pointer[index - 1]}' at index '{index - 1}' to be valid.");
                    }
                }
            }

            if (nodes.count == 0) return Result.Failure("Expected valid json.");
            return nodes.Pop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryHex(char character, out int value)
        {
            switch (character)
            {
                case _0: value = 0; return true;
                case _1: value = 1; return true;
                case _2: value = 2; return true;
                case _3: value = 3; return true;
                case _4: value = 4; return true;
                case _5: value = 5; return true;
                case _6: value = 6; return true;
                case _7: value = 7; return true;
                case _8: value = 8; return true;
                case _9: value = 9; return true;
                case _A: case _a: value = 10; return true;
                case _B: case _b: value = 11; return true;
                case _C: case _c: value = 12; return true;
                case _D: case _d: value = 13; return true;
                case _E: case _e: value = 14; return true;
                case _F: case _f: value = 15; return true;
                default: value = default; return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryHex(char partA, char partB, char partC, char partD, out int value)
        {
            if (TryHex(partA, out var valueA) && TryHex(partB, out var valueB) &&
                TryHex(partC, out var valueC) && TryHex(partD, out var valueD))
            {
                value = valueA | (valueB << 8) | (valueC << 16) | (valueD << 24);
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryDigit(char character, out double digit)
        {
            switch (character)
            {
                case _0: digit = 0d; return true;
                case _1: digit = 1d; return true;
                case _2: digit = 2d; return true;
                case _3: digit = 3d; return true;
                case _4: digit = 4d; return true;
                case _5: digit = 5d; return true;
                case _6: digit = 6d; return true;
                case _7: digit = 7d; return true;
                case _8: digit = 8d; return true;
                case _9: digit = 9d; return true;
                default: digit = default; return false;
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