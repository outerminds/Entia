using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Entia.Core;

namespace Entia.Json
{
    public static partial class Serialization
    {
        const char _a = 'a', _b = 'b', _c = 'c', _d = 'd', _e = 'e', _f = 'f';
        const char _A = 'A', _B = 'B', _C = 'C', _D = 'D', _E = 'E', _F = 'F', _N = 'N', _T = 'T';
        const char _l = 'l', _n = 'n', _r = 'r', _s = 's', _t = 't', _u = 'u';
        const char _0 = '0', _1 = '1', _2 = '2', _3 = '3', _4 = '4', _5 = '5', _6 = '6', _7 = '7', _8 = '8', _9 = '9';
        const char _plus = '+', _minus = '-', _comma = ',', _dot = '.', _colon = ':', _quote = '"', _backSlash = '\\', _frontSlash = '/';
        const char _openCurly = '{', _closeCurly = '}', _openSquare = '[', _closeSquare = ']';
        const char _tab = '\t', _space = ' ', _line = '\n', _return = '\r', _back = '\b', _feed = '\f';

        static readonly Node[] _empty = { };
        static readonly double[] _positives =
        {
            1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9,
            1e10, 1e11, 1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19,
            1e20, 1e21, 1e22, 1e23, 1e24, 1e25, 1e26, 1e27, 1e28, 1e29,
            1e30, 1e31, 1e32, 1e33, 1e34, 1e35, 1e36, 1e37, 1e38, 1e39,
            1e40, 1e41, 1e42, 1e43, 1e44, 1e45, 1e46, 1e47, 1e48, 1e49,
            1e50, 1e51, 1e52, 1e53, 1e54, 1e55, 1e56, 1e57, 1e58, 1e59,
            1e60, 1e61, 1e62, 1e63, 1e64, 1e65, 1e66, 1e67, 1e68, 1e69,
            1e70, 1e71, 1e72, 1e73, 1e74, 1e75, 1e76, 1e77, 1e78, 1e79,
            1e80, 1e81, 1e82, 1e83, 1e84, 1e85, 1e86, 1e87, 1e88, 1e89,
            1e90, 1e91, 1e92, 1e93, 1e94, 1e95, 1e96, 1e97, 1e98, 1e99,

            1e100, 1e101, 1e102, 1e103, 1e104, 1e105, 1e106, 1e107, 1e108, 1e109,
            1e110, 1e111, 1e112, 1e113, 1e114, 1e115, 1e116, 1e117, 1e118, 1e119,
            1e120, 1e121, 1e122, 1e123, 1e124, 1e125, 1e126, 1e127, 1e128, 1e129,
            1e130, 1e131, 1e132, 1e133, 1e134, 1e135, 1e136, 1e137, 1e138, 1e139,
            1e140, 1e141, 1e142, 1e143, 1e144, 1e145, 1e146, 1e147, 1e148, 1e149,
            1e150, 1e151, 1e152, 1e153, 1e154, 1e155, 1e156, 1e157, 1e158, 1e159,
            1e160, 1e161, 1e162, 1e163, 1e164, 1e165, 1e166, 1e167, 1e168, 1e169,
            1e170, 1e171, 1e172, 1e173, 1e174, 1e175, 1e176, 1e177, 1e178, 1e179,
            1e180, 1e181, 1e182, 1e183, 1e184, 1e185, 1e186, 1e187, 1e188, 1e189,
            1e190, 1e191, 1e192, 1e193, 1e194, 1e195, 1e196, 1e197, 1e198, 1e199,

            1e200, 1e201, 1e202, 1e203, 1e204, 1e205, 1e206, 1e207, 1e208, 1e209,
            1e210, 1e211, 1e212, 1e213, 1e214, 1e215, 1e216, 1e217, 1e218, 1e219,
            1e220, 1e221, 1e222, 1e223, 1e224, 1e225, 1e226, 1e227, 1e228, 1e229,
            1e230, 1e231, 1e232, 1e233, 1e234, 1e235, 1e236, 1e237, 1e238, 1e239,
            1e240, 1e241, 1e242, 1e243, 1e244, 1e245, 1e246, 1e247, 1e248, 1e249,
            1e250, 1e251, 1e252, 1e253, 1e254, 1e255, 1e256, 1e257, 1e258, 1e259,
            1e260, 1e261, 1e262, 1e263, 1e264, 1e265, 1e266, 1e267, 1e268, 1e269,
            1e270, 1e271, 1e272, 1e273, 1e274, 1e275, 1e276, 1e277, 1e278, 1e279,
            1e280, 1e281, 1e282, 1e283, 1e284, 1e285, 1e286, 1e287, 1e288, 1e289,
            1e290, 1e291, 1e292, 1e293, 1e294, 1e295, 1e296, 1e297, 1e298, 1e299,

            1e300, 1e301, 1e302, 1e303, 1e304, 1e305, 1e306, 1e307, 1e308,
        };
        static readonly double[] _negatives =
        {
            1e-0, 1e-1, 1e-2, 1e-3, 1e-4, 1e-5, 1e-6, 1e-7, 1e-8, 1e-9,
            1e-10, 1e-11, 1e-12, 1e-13, 1e-14, 1e-15, 1e-16, 1e-17, 1e-18, 1e-19,
            1e-20, 1e-21, 1e-22, 1e-23, 1e-24, 1e-25, 1e-26, 1e-27, 1e-28, 1e-29,
            1e-30, 1e-31, 1e-32, 1e-33, 1e-34, 1e-35, 1e-36, 1e-37, 1e-38, 1e-39,
            1e-40, 1e-41, 1e-42, 1e-43, 1e-44, 1e-45, 1e-46, 1e-47, 1e-48, 1e-49,
            1e-50, 1e-51, 1e-52, 1e-53, 1e-54, 1e-55, 1e-56, 1e-57, 1e-58, 1e-59,
            1e-60, 1e-61, 1e-62, 1e-63, 1e-64, 1e-65, 1e-66, 1e-67, 1e-68, 1e-69,
            1e-70, 1e-71, 1e-72, 1e-73, 1e-74, 1e-75, 1e-76, 1e-77, 1e-78, 1e-79,
            1e-80, 1e-81, 1e-82, 1e-83, 1e-84, 1e-85, 1e-86, 1e-87, 1e-88, 1e-89,
            1e-90, 1e-91, 1e-92, 1e-93, 1e-94, 1e-95, 1e-96, 1e-97, 1e-98, 1e-99,

            1e-100, 1e-101, 1e-102, 1e-103, 1e-104, 1e-105, 1e-106, 1e-107, 1e-108, 1e-109,
            1e-110, 1e-111, 1e-112, 1e-113, 1e-114, 1e-115, 1e-116, 1e-117, 1e-118, 1e-119,
            1e-120, 1e-121, 1e-122, 1e-123, 1e-124, 1e-125, 1e-126, 1e-127, 1e-128, 1e-129,
            1e-130, 1e-131, 1e-132, 1e-133, 1e-134, 1e-135, 1e-136, 1e-137, 1e-138, 1e-139,
            1e-140, 1e-141, 1e-142, 1e-143, 1e-144, 1e-145, 1e-146, 1e-147, 1e-148, 1e-149,
            1e-150, 1e-151, 1e-152, 1e-153, 1e-154, 1e-155, 1e-156, 1e-157, 1e-158, 1e-159,
            1e-160, 1e-161, 1e-162, 1e-163, 1e-164, 1e-165, 1e-166, 1e-167, 1e-168, 1e-169,
            1e-170, 1e-171, 1e-172, 1e-173, 1e-174, 1e-175, 1e-176, 1e-177, 1e-178, 1e-179,
            1e-180, 1e-181, 1e-182, 1e-183, 1e-184, 1e-185, 1e-186, 1e-187, 1e-188, 1e-189,
            1e-190, 1e-191, 1e-192, 1e-193, 1e-194, 1e-195, 1e-196, 1e-197, 1e-198, 1e-199,

            1e-200, 1e-201, 1e-202, 1e-203, 1e-204, 1e-205, 1e-206, 1e-207, 1e-208, 1e-209,
            1e-210, 1e-211, 1e-212, 1e-213, 1e-214, 1e-215, 1e-216, 1e-217, 1e-218, 1e-219,
            1e-220, 1e-221, 1e-222, 1e-223, 1e-224, 1e-225, 1e-226, 1e-227, 1e-228, 1e-229,
            1e-230, 1e-231, 1e-232, 1e-233, 1e-234, 1e-235, 1e-236, 1e-237, 1e-238, 1e-239,
            1e-240, 1e-241, 1e-242, 1e-243, 1e-244, 1e-245, 1e-246, 1e-247, 1e-248, 1e-249,
            1e-250, 1e-251, 1e-252, 1e-253, 1e-254, 1e-255, 1e-256, 1e-257, 1e-258, 1e-259,
            1e-260, 1e-261, 1e-262, 1e-263, 1e-264, 1e-265, 1e-266, 1e-267, 1e-268, 1e-269,
            1e-270, 1e-271, 1e-272, 1e-273, 1e-274, 1e-275, 1e-276, 1e-277, 1e-278, 1e-279,
            1e-280, 1e-281, 1e-282, 1e-283, 1e-284, 1e-285, 1e-286, 1e-287, 1e-288, 1e-289,
            1e-290, 1e-291, 1e-292, 1e-293, 1e-294, 1e-295, 1e-296, 1e-297, 1e-298, 1e-299,

            1e-300, 1e-301, 1e-302, 1e-303, 1e-304, 1e-305, 1e-306, 1e-307, 1e-308,
        };

        public static Result<Node> Parse(string text) => Parse(text, out _, out _);

        static unsafe Result<Node> Parse(string text, out Dictionary<int, uint> identifiers, out Dictionary<uint, object> references)
        {
            identifiers = new Dictionary<int, uint>();
            references = new Dictionary<uint, object>();
            var index = 0;
            var nodes = new Node[64];
            var brackets = new Stack<int>(8);
            var builder = default(StringBuilder);

            void Push(Node node)
            {
                if (index >= nodes.Length)
                {
                    var resized = new Node[nodes.Length * 2];
                    Array.Copy(nodes, 0, resized, 0, nodes.Length);
                    nodes = resized;
                }
                nodes[index++] = node;
            }

            Node[] Pop(int count)
            {
                if (count == 0) return _empty;
                index -= count;
                var popped = new Node[count];
                Array.Copy(nodes, index, popped, 0, count);
                return popped;
            }

            fixed (char* pointer = text)
            {
                var head = pointer;
                var tail = pointer + text.Length;
                while (head != tail)
                {
                    switch (*head++)
                    {
                        case _n:
                        case _N:
                            if (tail - head >= 3 && *head++ == _u && *head++ == _l && *head++ == _l)
                                Push(Node.Null);
                            else
                                return Result.Failure($"Expected 'null' at index '{Index(pointer, head) - 1}'.");
                            break;
                        case _t:
                        case _T:
                            if (tail - head >= 3 && *head++ == _r && *head++ == _u && *head++ == _e)
                                Push(Node.True);
                            else
                                return Result.Failure($"Expected 'true' at index '{Index(pointer, head) - 1}'.");
                            break;
                        case _f:
                        case _F:
                            if (tail - head >= 4 && *head++ == _a && *head++ == _l && *head++ == _s && *head++ == _e)
                                Push(Node.False);
                            else
                                return Result.Failure($"Expected 'false' at index '{Index(pointer, head) - 1}'.");
                            break;
                        case _minus: Push(Node.Number(ParseNumber(ref head, tail, 0, -1))); break;
                        case _0: Push(Node.Number(ParseNumber(ref head, tail, 0, 1))); break;
                        case _1: Push(Node.Number(ParseNumber(ref head, tail, 1, 1))); break;
                        case _2: Push(Node.Number(ParseNumber(ref head, tail, 2, 1))); break;
                        case _3: Push(Node.Number(ParseNumber(ref head, tail, 3, 1))); break;
                        case _4: Push(Node.Number(ParseNumber(ref head, tail, 4, 1))); break;
                        case _5: Push(Node.Number(ParseNumber(ref head, tail, 5, 1))); break;
                        case _6: Push(Node.Number(ParseNumber(ref head, tail, 6, 1))); break;
                        case _7: Push(Node.Number(ParseNumber(ref head, tail, 7, 1))); break;
                        case _8: Push(Node.Number(ParseNumber(ref head, tail, 8, 1))); break;
                        case _9: Push(Node.Number(ParseNumber(ref head, tail, 9, 1))); break;
                        case _quote:
                            {
                                var start = head;
                                while (head != tail)
                                {
                                    switch (*head++)
                                    {
                                        case _backSlash:
                                            if (builder == null) builder = new StringBuilder(256);
                                            else builder.Clear();
                                            // NOTE: this string is not plain since at least 1 character was unescaped
                                            Push(Node.String(builder.Unescape(ref head, tail, ref start), false));
                                            break;
                                        case _quote:
                                            Push(Node.String(new string(start, 0, Index(start, head) - 1), true));
                                            break;
                                        default: continue;
                                    }
                                    break;
                                }
                                break;
                            }
                        case _openCurly:
                        case _openSquare: brackets.Push(index); break;
                        case _closeCurly:
                            if (brackets.TryPop(out var memberCount))
                            {
                                Push(Node.Object(Pop(index - memberCount)));
                                break;
                            }
                            else
                                return Result.Failure($"Expected balanced curly bracket at index '{Index(pointer, head) - 1}'.");
                        case _closeSquare:
                            if (brackets.TryPop(out var itemCount))
                            {
                                Push(Node.Array(Pop(index - itemCount)));
                                break;
                            }
                            else
                                return Result.Failure($"Expected balanced square bracket at index '{Index(pointer, head) - 1}'.");
                        case _space: case _tab: case _line: case _return: case _comma: case _colon: break;
                        default: return Result.Failure($"Expected character '{head[1]}' at index '{Index(pointer, head) - 1}' to be valid.");
                    }
                }
            }

            if (index == 0) return Result.Failure("Expected valid json.");
            var node = nodes[0];
            Unwrap(ref node, identifiers, references);
            return node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe int Index(char* head, char* tail) => (int)(tail - head);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe string Unescape(this StringBuilder builder, ref char* head, char* tail, ref char* start)
        {
            builder.AppendUnescaped(ref head, tail, ref start);
            while (head != tail)
            {
                var current = *head++;
                if (current == _backSlash) builder.AppendUnescaped(ref head, tail, ref start);
                else if (current == _quote) break;
            }
            builder.Append(start, Index(start, head) - 1);
            return builder.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void AppendUnescaped(this StringBuilder builder, ref char* head, char* tail, ref char* start)
        {
            builder.Append(start, Index(start, head) - 1);
            switch (*head++)
            {
                case _n: builder.Append(_line); break;
                case _b: builder.Append(_back); break;
                case _f: builder.Append(_feed); break;
                case _r: builder.Append(_return); break;
                case _t: builder.Append(_tab); break;
                case _quote: builder.Append(_quote); break;
                case _backSlash: builder.Append(_backSlash); break;
                case _frontSlash: builder.Append(_frontSlash); break;
                case _u:
                    if (tail - head >= 4)
                    {
                        var value =
                            (FromHex(*head++) << 12) |
                            (FromHex(*head++) << 8) |
                            (FromHex(*head++) << 4) |
                            FromHex(*head++);
                        builder.Append((char)value);
                    }
                    break;
            }
            start = head++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int FromHex(char character)
        {
            if (character >= _0 && character <= _9) return character - _0;
            else if (character >= _a && character <= _f) return character - _a + 10;
            else if (character >= _A && character <= _F) return character - _A + 10;
            else return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe char ParseInteger(ref char* head, char* tail, ref long value)
        {
            while (head != tail)
            {
                var character = *head;
                if (character >= _0 && character <= _9)
                {
                    head++;
                    value = value * 10 + (character - _0);
                }
                else return character;
            }
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe object ParseNumber(ref char* head, char* tail, long value, long sign)
        {
            var character = ParseInteger(ref head, tail, ref value);
            if (character == _dot)
            {
                head++;
                return ParseFraction(ref head, tail, value) * sign;
            }
            else if (character == _e || character == _E)
            {
                head++;
                return ParseExponent(ref head, tail, value) * sign;
            }
            else return value * sign;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe double ParseFraction(ref char* head, char* tail, double value)
        {
            var start = head;
            var fraction = 0L;
            var character = ParseInteger(ref head, tail, ref fraction);
            if (character == _e || character == _E)
            {
                head++;
                return ParseExponent(ref head, tail, value + fraction / _positives[Index(start, head)]);
            }
            else return value + fraction / _positives[Index(start, head)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe double ParseExponent(ref char* head, char* tail, double value)
        {
            var exponent = 0u;
            var powers = _positives;
            while (head != tail)
            {
                switch (*head)
                {
                    case _0: head++; exponent *= 10u; continue;
                    case _1: head++; exponent = exponent * 10u + 1u; continue;
                    case _2: head++; exponent = exponent * 10u + 2u; continue;
                    case _3: head++; exponent = exponent * 10u + 3u; continue;
                    case _4: head++; exponent = exponent * 10u + 4u; continue;
                    case _5: head++; exponent = exponent * 10u + 5u; continue;
                    case _6: head++; exponent = exponent * 10u + 6u; continue;
                    case _7: head++; exponent = exponent * 10u + 7u; continue;
                    case _8: head++; exponent = exponent * 10u + 8u; continue;
                    case _9: head++; exponent = exponent * 10u + 9u; continue;
                    case _plus: head++; powers = _positives; continue;
                    case _minus: head++; powers = _negatives; continue;
                }
                break;
            }
            return value * powers[exponent];
        }

        static void Unwrap(ref Node node, Dictionary<int, uint> identifiers, Dictionary<uint, object> references)
        {
            static bool TryIdentifier(Node node, out int index, out Node value)
            {
                if (node.Children.Length == 4 &&
                    node.Children[0].AsString() == "$i" &&
                    node.Children[1].TryInt(out index) &&
                    node.Children[2].AsString() == "$v")
                {
                    value = node.Children[3];
                    return true;
                }
                index = default;
                value = default;
                return false;
            }

            static bool TryReference(Node node, out int reference)
            {
                if (node.Children.Length == 2 && node.Children[0].AsString() == "$r")
                    return node.Children[1].TryInt(out reference);
                reference = default;
                return false;
            }

            static bool TryType(Node node, Dictionary<uint, object> references, out Node type)
            {
                if (node.Children.Length == 2 && node.Children[0].AsString() == "$t")
                {
                    type = Node.Type(JsonUtility.NodeToType(node.Children[1], references)).With(node.Identifier);
                    return true;
                }
                type = default;
                return false;
            }

            static bool TryAbstract(Node node, Dictionary<uint, object> references, out Node @abstract)
            {
                if (node.Children.Length == 4 &&
                    node.Children[0].AsString() == "$a" &&
                    node.Children[2].AsString() == "$v")
                {
                    var value = node.Children[1];
                    @abstract = Node.Abstract(
                        Node.Type(JsonUtility.NodeToType(value, references)).With(value.Identifier),
                        node.Children[3]).With(node.Identifier);
                    return true;
                }
                @abstract = default;
                return false;
            }

            if (TryIdentifier(node, out var index, out var value))
            {
                identifiers[index] = value.Identifier;
                node = value;
            }

            if (TryReference(node, out index))
                node = identifiers.TryGetValue(index, out var reference) ?
                    Node.Reference(reference) : Node.Null;

            for (int i = 0; i < node.Children.Length; i++) Unwrap(ref node.Children[i], identifiers, references);

            if (TryType(node, references, out var type)) node = type;
            else if (TryAbstract(node, references, out var @abstract)) node = @abstract;
        }
    }
}