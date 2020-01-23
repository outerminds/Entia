using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Entia.Core;

namespace Entia.Json
{
    public sealed class Node
    {
        public enum Kinds { Null, Boolean, Number, String, Object, Array }

        public static implicit operator Node(bool value) => Boolean(value);
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
        public static implicit operator Node(char value) => String(value);
        public static implicit operator Node(string value) => String(value);

        public static readonly Node Null = new Node(Kinds.Null, "null");
        public static readonly Node True = new Node(Kinds.Boolean, bool.TrueString.ToLower());
        public static readonly Node False = new Node(Kinds.Boolean, bool.FalseString.ToLower());
        public static readonly Node Zero = new Node(Kinds.Number, "0");

        static readonly Node[] _empty = { };

        public static Node Boolean(bool value) => value ? True : False;
        public static Node Number(byte value) => new Node(Kinds.Number, value.ToString());
        public static Node Number(sbyte value) => new Node(Kinds.Number, value.ToString());
        public static Node Number(short value) => new Node(Kinds.Number, value.ToString());
        public static Node Number(ushort value) => new Node(Kinds.Number, value.ToString());
        public static Node Number(int value) => new Node(Kinds.Number, value.ToString());
        public static Node Number(uint value) => new Node(Kinds.Number, value.ToString());
        public static Node Number(long value) => new Node(Kinds.Number, value.ToString());
        public static Node Number(ulong value) => new Node(Kinds.Number, value.ToString());
        public static Node Number(float value) => float.IsNaN(value) || float.IsInfinity(value) ? Zero : new Node(Kinds.Number, value.ToString());
        public static Node Number(double value) => double.IsNaN(value) || double.IsInfinity(value) ? Zero : new Node(Kinds.Number, value.ToString());
        public static Node Number(decimal value) => new Node(Kinds.Number, value.ToString());
        public static Node String(char value) => new Node(Kinds.String, value.ToString());
        public static Node String(string value) => value is null ? Null : new Node(Kinds.String, value);
        public static Node Array(params Node[] items) => new Node(Kinds.Array, items);
        public static Node Object(params Node[] members) => new Node(Kinds.Object, members);
        public static Node Object(params (string key, Node value)[] members)
        {
            var children = new Node[members.Length * 2];
            for (int i = 0; i < members.Length; i++)
                (children[i * 2], children[i * 2 + 1]) = members[i];
            return Object(children);
        }
        public static Node Abstract(Node type, Node value) => Object("$t", type, "$v", value);
        public static Node Reference(int reference) => Object("$r", reference);

        public readonly Kinds Kind;
        public readonly string Value;
        public readonly Node[] Children;

        public Node(Kinds kind, params Node[] children)
        {
            Kind = kind;
            Value = "";
            Children = children;
        }

        public Node(Kinds kind, string value)
        {
            Kind = kind;
            Value = value;
            Children = _empty;
        }

        public override string ToString() => Serialization.Generate(this);
    }

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

        public static Node With(this Node node, params Node[] children) => new Node(node.Kind, children);
        public static Node With(this Node node, string value) => new Node(node.Kind, value);
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
        public static bool IsNull(this Node node) => node.Is(Node.Kinds.Null);
        public static bool IsString(this Node node) => node.Is(Node.Kinds.String);
        public static bool IsBoolean(this Node node) => node.Is(Node.Kinds.Boolean);
        public static bool IsNumber(this Node node) => node.Is(Node.Kinds.Number);
        public static bool IsArray(this Node node) => node.Is(Node.Kinds.Array);
        public static bool IsObject(this Node node) => node.Is(Node.Kinds.Object) && node.Children.Length % 2 == 0;
        public static bool IsNull(this Node node, string key) => node.Is(key, Node.Kinds.Null);
        public static bool IsString(this Node node, string key) => node.Is(key, Node.Kinds.String);
        public static bool IsBoolean(this Node node, string key) => node.Is(key, Node.Kinds.Boolean);
        public static bool IsNumber(this Node node, string key) => node.Is(key, Node.Kinds.Number);
        public static bool IsArray(this Node node, string key) => node.Is(key, Node.Kinds.Array);
        public static bool IsObject(this Node node, string key) => node.Is(key, Node.Kinds.Object);
        public static bool IsNull(this Node node, int index) => node.Is(index, Node.Kinds.Null);
        public static bool IsString(this Node node, int index) => node.Is(index, Node.Kinds.String);
        public static bool IsBoolean(this Node node, int index) => node.Is(index, Node.Kinds.Boolean);
        public static bool IsNumber(this Node node, int index) => node.Is(index, Node.Kinds.Number);
        public static bool IsArray(this Node node, int index) => node.Is(index, Node.Kinds.Array);
        public static bool IsObject(this Node node, int index) => node.Is(index, Node.Kinds.Object);

        public static bool TryString(this Node node, out string value)
        {
            value = node.Value;
            return node.IsString();
        }

        public static bool TryBool(this Node node, out bool value)
        {
            if (node.IsBoolean()) return bool.TryParse(node.Value, out value);
            value = default;
            return false;
        }

        public static bool TryChar(this Node node, out char value)
        {
            if (node.TryString(out var @string) && @string.Length > 0)
            {
                value = node.Value[0];
                return true;
            }
            else if (node.TryInt(out var @int))
            {
                value = (char)@int;
                return true;
            }
            value = default;
            return false;
        }

        public static bool TrySByte(this Node node, out sbyte value)
        {
            if (node.IsNumber()) return sbyte.TryParse(node.Value, out value);
            value = default;
            return false;
        }

        public static bool TryByte(this Node node, out byte value)
        {
            if (node.IsNumber()) return byte.TryParse(node.Value, out value);
            value = default;
            return false;
        }

        public static bool TryShort(this Node node, out short value)
        {
            if (node.IsNumber()) return short.TryParse(node.Value, out value);
            value = default;
            return false;
        }

        public static bool TryUShort(this Node node, out ushort value)
        {
            if (node.IsNumber()) return ushort.TryParse(node.Value, out value);
            value = default;
            return false;
        }

        public static bool TryInt(this Node node, out int value)
        {
            if (node.IsNumber()) return int.TryParse(node.Value, out value);
            value = default;
            return false;
        }

        public static bool TryUInt(this Node node, out uint value)
        {
            if (node.IsNumber()) return uint.TryParse(node.Value, out value);
            value = default;
            return false;
        }

        public static bool TryLong(this Node node, out long value)
        {
            if (node.IsNumber()) return long.TryParse(node.Value, out value);
            value = default;
            return false;
        }

        public static bool TryULong(this Node node, out ulong value)
        {
            if (node.IsNumber()) return ulong.TryParse(node.Value, out value);
            value = default;
            return false;
        }

        public static bool TryFloat(this Node node, out float value)
        {
            if (node.IsNumber()) return float.TryParse(node.Value, out value);
            value = default;
            return false;
        }

        public static bool TryDouble(this Node node, out double value)
        {
            if (node.IsNumber()) return double.TryParse(node.Value, out value);
            value = default;
            return false;
        }

        public static bool TryDecimal(this Node node, out decimal value)
        {
            if (node.IsNumber()) return decimal.TryParse(node.Value, out value);
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

        public static string AsString(this Node node) => node.TryString(out var value) ? value : default;
        public static bool AsBool(this Node node) => node.TryBool(out var value) ? value : default;
        public static char AsChar(this Node node) => node.TryChar(out var value) ? value : default;
        public static sbyte AsSByte(this Node node) => node.TrySByte(out var value) ? value : default;
        public static byte AsByte(this Node node) => node.TryByte(out var value) ? value : default;
        public static short AsShort(this Node node) => node.TryShort(out var value) ? value : default;
        public static ushort AsUShort(this Node node) => node.TryUShort(out var value) ? value : default;
        public static int AsInt(this Node node) => node.TryInt(out var value) ? value : default;
        public static uint AsUInt(this Node node) => node.TryUInt(out var value) ? value : default;
        public static long AsLong(this Node node) => node.TryLong(out var value) ? value : default;
        public static ulong AsULong(this Node node) => node.TryULong(out var value) ? value : default;
        public static float AsFloat(this Node node) => node.TryFloat(out var value) ? value : default;
        public static double AsDouble(this Node node) => node.TryDouble(out var value) ? value : default;
        public static decimal AsDecimal(this Node node) => node.TryDecimal(out var value) ? value : default;
        public static string AsString(this Node node, string key) => node.TryString(key, out var value) ? value : default;
        public static bool AsBool(this Node node, string key) => node.TryBool(key, out var value) ? value : default;
        public static char AsChar(this Node node, string key) => node.TryChar(key, out var value) ? value : default;
        public static sbyte AsSByte(this Node node, string key) => node.TrySByte(key, out var value) ? value : default;
        public static byte AsByte(this Node node, string key) => node.TryByte(key, out var value) ? value : default;
        public static short AsShort(this Node node, string key) => node.TryShort(key, out var value) ? value : default;
        public static ushort AsUShort(this Node node, string key) => node.TryUShort(key, out var value) ? value : default;
        public static int AsInt(this Node node, string key) => node.TryInt(key, out var value) ? value : default;
        public static uint AsUInt(this Node node, string key) => node.TryUInt(key, out var value) ? value : default;
        public static long AsLong(this Node node, string key) => node.TryLong(key, out var value) ? value : default;
        public static ulong AsULong(this Node node, string key) => node.TryULong(key, out var value) ? value : default;
        public static float AsFloat(this Node node, string key) => node.TryFloat(key, out var value) ? value : default;
        public static double AsDouble(this Node node, string key) => node.TryDouble(key, out var value) ? value : default;
        public static decimal AsDecimal(this Node node, string key) => node.TryDecimal(key, out var value) ? value : default;
        public static string AsString(this Node node, int index) => node.TryString(index, out var value) ? value : default;
        public static bool AsBool(this Node node, int index) => node.TryBool(index, out var value) ? value : default;
        public static char AsChar(this Node node, int index) => node.TryChar(index, out var value) ? value : default;
        public static sbyte AsSByte(this Node node, int index) => node.TrySByte(index, out var value) ? value : default;
        public static byte AsByte(this Node node, int index) => node.TryByte(index, out var value) ? value : default;
        public static short AsShort(this Node node, int index) => node.TryShort(index, out var value) ? value : default;
        public static ushort AsUShort(this Node node, int index) => node.TryUShort(index, out var value) ? value : default;
        public static int AsInt(this Node node, int index) => node.TryInt(index, out var value) ? value : default;
        public static uint AsUInt(this Node node, int index) => node.TryUInt(index, out var value) ? value : default;
        public static long AsLong(this Node node, int index) => node.TryLong(index, out var value) ? value : default;
        public static ulong AsULong(this Node node, int index) => node.TryULong(index, out var value) ? value : default;
        public static float AsFloat(this Node node, int index) => node.TryFloat(index, out var value) ? value : default;
        public static double AsDouble(this Node node, int index) => node.TryDouble(index, out var value) ? value : default;
        public static decimal AsDecimal(this Node node, int index) => node.TryDecimal(index, out var value) ? value : default;

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
            if (node.IsObject() && node.Children.Length == 1 &&
                node.Children[0].TryMember("$r", out var member) &&
                member.TryInt(out reference))
                return true;
            reference = default;
            return false;
        }

        public static bool TryAbstract(this Node node, out Node type, out Node value)
        {
            if (node.IsObject() && node.Children.Length == 2 &&
                node.Children[0].TryMember("$t", out type) &&
                node.Children[1].TryMember("$v", out value))
                return true;
            type = default;
            value = default;
            return false;
        }
    }
}