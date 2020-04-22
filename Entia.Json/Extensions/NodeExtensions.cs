using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Entia.Core;

namespace Entia.Json
{
    public static class NodeExtensions
    {
        public readonly struct MemberEnumerable : IEnumerable<MemberEnumerator, (string key, Node value)>
        {
            readonly Node _node;
            public MemberEnumerable(Node node) { _node = node; }
            public MemberEnumerator GetEnumerator() => new MemberEnumerator(_node);
            IEnumerator<(string key, Node value)> IEnumerable<(string key, Node value)>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct MemberEnumerator : IEnumerator<(string key, Node value)>
        {
            public (string key, Node value) Current { get; private set; }
            object IEnumerator.Current => Current;

            readonly Node[] _nodes;
            int _index;

            public MemberEnumerator(Node node)
            {
                Current = default;
                if (node.IsObject())
                {
                    _nodes = node.Children;
                    _index = 0;
                }
                else
                {
                    _nodes = Array.Empty<Node>();
                    _index = 0;
                }
            }

            public bool MoveNext()
            {
                if (_index < _nodes.Length && _nodes[_index].TryString(out var key))
                {
                    Current = (key, _nodes[_index + 1]);
                    _index += 2;
                    return true;
                }
                return false;
            }

            public void Reset() => _index = 0;
            public void Dispose() => this = default;
        }

        public static Node With(this Node node, params Node[] children) => new Node(node.Kind, node.Tag, node.Value, children);
        public static Node WithMember(this Node node, string key, Node value)
        {
            if (node.IsObject())
            {
                foreach (var pair in node.Members())
                    if (pair.key == key) return node.WithReplacement(pair.value, value);
                return node.With(node.Children.Append(key, value));
            }
            return node;
        }

        public static Node Without(this Node node, params Node[] children) =>
            node.With(node.Children.Except(children).ToArray());

        public static Node WithReplacement(this Node node, Node child, Node replacement) =>
            node.WithReplacement(Array.IndexOf(node.Children, child), replacement);

        public static Node WithReplacement(this Node node, int index, Node replacement)
        {
            if (index < 0 || index >= node.Children.Length) return node;
            var children = node.Children.Clone() as Node[];
            children[index] = replacement;
            return node.With(children);
        }

        public static bool Is(this Node node, Node.Kinds kind) => node.Kind == kind;
        public static bool Is(this Node node, string key, Node.Kinds kind) => node.TryMember(key, out var value) && value.Is(kind);
        public static bool Is(this Node node, int index, Node.Kinds kind) => node.TryItem(index, out var item) && item.Is(kind);
        public static bool Has(this Node node, Node.Tags tag) => (node.Tag & tag) == tag;
        public static bool Has(this Node node, string key, Node.Tags tag) => node.TryMember(key, out var value) && value.Has(tag);
        public static bool Has(this Node node, int index, Node.Tags tag) => node.TryItem(index, out var item) && item.Has(tag);
        public static bool HasPlain(this Node node) => node.Has(Node.Tags.Plain);
        public static bool IsNull(this Node node) => node.Is(Node.Kinds.Null);
        public static bool IsString(this Node node) => node.Is(Node.Kinds.String);
        public static bool IsBoolean(this Node node) => node.Is(Node.Kinds.Boolean);
        public static bool IsNumber(this Node node) => node.Is(Node.Kinds.Number);
        public static bool IsArray(this Node node) => node.Is(Node.Kinds.Array);
        public static bool IsObject(this Node node) => node.Is(Node.Kinds.Object) && node.Children.Length % 2 == 0;
        public static bool HasPlain(this Node node, string key) => node.Has(key, Node.Tags.Plain);
        public static bool IsNull(this Node node, string key) => node.Is(key, Node.Kinds.Null);
        public static bool IsString(this Node node, string key) => node.Is(key, Node.Kinds.String);
        public static bool IsBoolean(this Node node, string key) => node.Is(key, Node.Kinds.Boolean);
        public static bool IsNumber(this Node node, string key) => node.Is(key, Node.Kinds.Number);
        public static bool IsArray(this Node node, string key) => node.Is(key, Node.Kinds.Array);
        public static bool IsObject(this Node node, string key) => node.Is(key, Node.Kinds.Object);
        public static bool HasPlain(this Node node, int index) => node.Has(index, Node.Tags.Plain);
        public static bool IsNull(this Node node, int index) => node.Is(index, Node.Kinds.Null);
        public static bool IsString(this Node node, int index) => node.Is(index, Node.Kinds.String);
        public static bool IsBoolean(this Node node, int index) => node.Is(index, Node.Kinds.Boolean);
        public static bool IsNumber(this Node node, int index) => node.Is(index, Node.Kinds.Number);
        public static bool IsArray(this Node node, int index) => node.Is(index, Node.Kinds.Array);
        public static bool IsObject(this Node node, int index) => node.Is(index, Node.Kinds.Object);

        public static bool TryString(this Node node, out string value)
        {
            if (node.IsString())
            {
                value = (string)node.Value;
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryBool(this Node node, out bool value)
        {
            if (node.IsBoolean())
            {
                value = (bool)node.Value;
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryChar(this Node node, out char value)
        {
            if (node.TryString(out var @string)) return @string.TryFirst(out value);
            else if (node.TryLong(out var @long))
            {
                value = (char)@long;
                return true;
            }
            value = default;
            return false;
        }

        public static bool TrySByte(this Node node, out sbyte value)
        {
            if (node.TryLong(out var number))
            {
                value = (sbyte)number;
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryByte(this Node node, out byte value)
        {
            if (node.TryLong(out var number))
            {
                value = (byte)number;
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryShort(this Node node, out short value)
        {
            if (node.TryLong(out var number))
            {
                value = (short)number;
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryUShort(this Node node, out ushort value)
        {
            if (node.TryLong(out var number))
            {
                value = (ushort)number;
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryInt(this Node node, out int value)
        {
            if (node.TryLong(out var number))
            {
                value = (int)number;
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryUInt(this Node node, out uint value)
        {
            if (node.TryLong(out var number))
            {
                value = (uint)number;
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryLong(this Node node, out long value)
        {
            if (node.IsNumber())
            {
                value =
                    node.Value is long @long ? @long :
                    node.Value is double @double ? (long)@double :
                    Convert.ToInt64(node.Value, CultureInfo.InvariantCulture);
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryULong(this Node node, out ulong value)
        {
            if (node.TryLong(out var number))
            {
                value = (ulong)number;
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryFloat(this Node node, out float value)
        {
            if (node.TryDouble(out var number))
            {
                value = (float)number;
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryDouble(this Node node, out double value)
        {
            if (node.IsNumber())
            {
                value =
                    node.Value is double @double ? @double :
                    node.Value is long @long ? @long :
                    Convert.ToDouble(node.Value, CultureInfo.InvariantCulture);
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryDecimal(this Node node, out decimal value)
        {
            if (node.TryDouble(out var number))
            {
                value = (decimal)number;
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryString(this Node node, string key, out string value)
        {
            if (node.TryMember(key, out var member)) return member.TryString(out value);
            value = default;
            return false;
        }

        public static bool TryBool(this Node node, string key, out bool value)
        {
            if (node.TryMember(key, out var member)) return member.TryBool(out value);
            value = default;
            return false;
        }

        public static bool TryChar(this Node node, string key, out char value)
        {
            if (node.TryMember(key, out var member)) return member.TryChar(out value);
            value = default;
            return false;
        }

        public static bool TrySByte(this Node node, string key, out sbyte value)
        {
            if (node.TryMember(key, out var member)) return member.TrySByte(out value);
            value = default;
            return false;
        }

        public static bool TryByte(this Node node, string key, out byte value)
        {
            if (node.TryMember(key, out var member)) return member.TryByte(out value);
            value = default;
            return false;
        }

        public static bool TryShort(this Node node, string key, out short value)
        {
            if (node.TryMember(key, out var member)) return member.TryShort(out value);
            value = default;
            return false;
        }

        public static bool TryUShort(this Node node, string key, out ushort value)
        {
            if (node.TryMember(key, out var member)) return member.TryUShort(out value);
            value = default;
            return false;
        }

        public static bool TryInt(this Node node, string key, out int value)
        {
            if (node.TryMember(key, out var member)) return member.TryInt(out value);
            value = default;
            return false;
        }

        public static bool TryUInt(this Node node, string key, out uint value)
        {
            if (node.TryMember(key, out var member)) return member.TryUInt(out value);
            value = default;
            return false;
        }

        public static bool TryLong(this Node node, string key, out long value)
        {
            if (node.TryMember(key, out var member)) return member.TryLong(out value);
            value = default;
            return false;
        }

        public static bool TryULong(this Node node, string key, out ulong value)
        {
            if (node.TryMember(key, out var member)) return member.TryULong(out value);
            value = default;
            return false;
        }

        public static bool TryFloat(this Node node, string key, out float value)
        {
            if (node.TryMember(key, out var member)) return member.TryFloat(out value);
            value = default;
            return false;
        }

        public static bool TryDouble(this Node node, string key, out double value)
        {
            if (node.TryMember(key, out var member)) return member.TryDouble(out value);
            value = default;
            return false;
        }

        public static bool TryDecimal(this Node node, string key, out decimal value)
        {
            if (node.TryMember(key, out var member)) return member.TryDecimal(out value);
            value = default;
            return false;
        }

        public static bool TryString(this Node node, int index, out string value)
        {
            if (node.TryItem(index, out var item)) return item.TryString(out value);
            value = default;
            return false;
        }

        public static bool TryBool(this Node node, int index, out bool value)
        {
            if (node.TryItem(index, out var item)) return item.TryBool(out value);
            value = default;
            return false;
        }

        public static bool TryChar(this Node node, int index, out char value)
        {
            if (node.TryItem(index, out var item)) return item.TryChar(out value);
            value = default;
            return false;
        }

        public static bool TrySByte(this Node node, int index, out sbyte value)
        {
            if (node.TryItem(index, out var item)) return item.TrySByte(out value);
            value = default;
            return false;
        }

        public static bool TryByte(this Node node, int index, out byte value)
        {
            if (node.TryItem(index, out var item)) return item.TryByte(out value);
            value = default;
            return false;
        }

        public static bool TryShort(this Node node, int index, out short value)
        {
            if (node.TryItem(index, out var item)) return item.TryShort(out value);
            value = default;
            return false;
        }

        public static bool TryUShort(this Node node, int index, out ushort value)
        {
            if (node.TryItem(index, out var item)) return item.TryUShort(out value);
            value = default;
            return false;
        }

        public static bool TryInt(this Node node, int index, out int value)
        {
            if (node.TryItem(index, out var item)) return item.TryInt(out value);
            value = default;
            return false;
        }

        public static bool TryUInt(this Node node, int index, out uint value)
        {
            if (node.TryItem(index, out var item)) return item.TryUInt(out value);
            value = default;
            return false;
        }

        public static bool TryLong(this Node node, int index, out long value)
        {
            if (node.TryItem(index, out var item)) return item.TryLong(out value);
            value = default;
            return false;
        }

        public static bool TryULong(this Node node, int index, out ulong value)
        {
            if (node.TryItem(index, out var item)) return item.TryULong(out value);
            value = default;
            return false;
        }

        public static bool TryFloat(this Node node, int index, out float value)
        {
            if (node.TryItem(index, out var item)) return item.TryFloat(out value);
            value = default;
            return false;
        }

        public static bool TryDouble(this Node node, int index, out double value)
        {
            if (node.TryItem(index, out var item)) return item.TryDouble(out value);
            value = default;
            return false;
        }

        public static bool TryDecimal(this Node node, int index, out decimal value)
        {
            if (node.TryItem(index, out var item)) return item.TryDecimal(out value);
            value = default;
            return false;
        }

        public static string AsString(this Node node, string @default = "") => node.TryString(out var value) ? value : @default;
        public static bool AsBool(this Node node, bool @default = default) => node.TryBool(out var value) ? value : @default;
        public static char AsChar(this Node node, char @default = default) => node.TryChar(out var value) ? value : @default;
        public static sbyte AsSByte(this Node node, sbyte @default = default) => node.TrySByte(out var value) ? value : @default;
        public static byte AsByte(this Node node, byte @default = default) => node.TryByte(out var value) ? value : @default;
        public static short AsShort(this Node node, short @default = default) => node.TryShort(out var value) ? value : @default;
        public static ushort AsUShort(this Node node, ushort @default = default) => node.TryUShort(out var value) ? value : @default;
        public static int AsInt(this Node node, int @default = default) => node.TryInt(out var value) ? value : @default;
        public static uint AsUInt(this Node node, uint @default = default) => node.TryUInt(out var value) ? value : @default;
        public static long AsLong(this Node node, long @default = default) => node.TryLong(out var value) ? value : @default;
        public static ulong AsULong(this Node node, ulong @default = default) => node.TryULong(out var value) ? value : @default;
        public static float AsFloat(this Node node, float @default = default) => node.TryFloat(out var value) ? value : @default;
        public static double AsDouble(this Node node, double @default = default) => node.TryDouble(out var value) ? value : @default;
        public static decimal AsDecimal(this Node node, decimal @default = default) => node.TryDecimal(out var value) ? value : @default;
        public static string AsString(this Node node, string key, string @default = "") => node.TryString(key, out var value) ? value : @default;
        public static bool AsBool(this Node node, string key, bool @default = default) => node.TryBool(key, out var value) ? value : @default;
        public static char AsChar(this Node node, string key, char @default = default) => node.TryChar(key, out var value) ? value : @default;
        public static sbyte AsSByte(this Node node, string key, sbyte @default = default) => node.TrySByte(key, out var value) ? value : @default;
        public static byte AsByte(this Node node, string key, byte @default = default) => node.TryByte(key, out var value) ? value : @default;
        public static short AsShort(this Node node, string key, short @default = default) => node.TryShort(key, out var value) ? value : @default;
        public static ushort AsUShort(this Node node, string key, ushort @default = default) => node.TryUShort(key, out var value) ? value : @default;
        public static int AsInt(this Node node, string key, int @default = default) => node.TryInt(key, out var value) ? value : @default;
        public static uint AsUInt(this Node node, string key, uint @default = default) => node.TryUInt(key, out var value) ? value : @default;
        public static long AsLong(this Node node, string key, long @default = default) => node.TryLong(key, out var value) ? value : @default;
        public static ulong AsULong(this Node node, string key, ulong @default = default) => node.TryULong(key, out var value) ? value : @default;
        public static float AsFloat(this Node node, string key, float @default = default) => node.TryFloat(key, out var value) ? value : @default;
        public static double AsDouble(this Node node, string key, double @default = default) => node.TryDouble(key, out var value) ? value : @default;
        public static decimal AsDecimal(this Node node, string key, decimal @default = default) => node.TryDecimal(key, out var value) ? value : @default;
        public static string AsString(this Node node, int index, string @default = "") => node.TryString(index, out var value) ? value : @default;
        public static bool AsBool(this Node node, int index, bool @default = default) => node.TryBool(index, out var value) ? value : @default;
        public static char AsChar(this Node node, int index, char @default = default) => node.TryChar(index, out var value) ? value : @default;
        public static sbyte AsSByte(this Node node, int index, sbyte @default = default) => node.TrySByte(index, out var value) ? value : @default;
        public static byte AsByte(this Node node, int index, byte @default = default) => node.TryByte(index, out var value) ? value : @default;
        public static short AsShort(this Node node, int index, short @default = default) => node.TryShort(index, out var value) ? value : @default;
        public static ushort AsUShort(this Node node, int index, ushort @default = default) => node.TryUShort(index, out var value) ? value : @default;
        public static int AsInt(this Node node, int index, int @default = default) => node.TryInt(index, out var value) ? value : @default;
        public static uint AsUInt(this Node node, int index, uint @default = default) => node.TryUInt(index, out var value) ? value : @default;
        public static long AsLong(this Node node, int index, long @default = default) => node.TryLong(index, out var value) ? value : @default;
        public static ulong AsULong(this Node node, int index, ulong @default = default) => node.TryULong(index, out var value) ? value : @default;
        public static float AsFloat(this Node node, int index, float @default = default) => node.TryFloat(index, out var value) ? value : @default;
        public static double AsDouble(this Node node, int index, double @default = default) => node.TryDouble(index, out var value) ? value : @default;
        public static decimal AsDecimal(this Node node, int index, decimal @default = default) => node.TryDecimal(index, out var value) ? value : @default;

        public static MemberEnumerable Members(this Node node) => new MemberEnumerable(node);
        public static bool TryMember(this Node node, string key, out Node value)
        {
            foreach (var pair in node.Members())
            {
                if (pair.key == key)
                {
                    value = pair.value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        public static bool TryItem(this Node node, int index, out Node item)
        {
            if (node.IsArray() && index >= 0 && index < node.Children.Length)
            {
                item = node.Children[index];
                return true;
            }
            item = default;
            return false;
        }

        public static Node[] Items(this Node node) => node.IsArray() ? node.Children : Array.Empty<Node>();

        public static bool TryReference(this Node node, out int reference)
        {
            if (node.IsObject() && node.Children.Length == 2 &&
                node.Children[0].AsString() == "$r" &&
                node.Children[1].TryInt(out reference))
                return true;
            reference = default;
            return false;
        }

        public static bool TryAbstract(this Node node, out Node type, out Node value)
        {
            if (node.IsObject() && node.Children.Length == 4 &&
                node.Children[0].AsString() == "$t" &&
                node.Children[2].AsString() == "$v")
            {
                type = node.Children[1];
                value = node.Children[3];
                return true;
            }

            type = default;
            value = default;
            return false;
        }
    }
}