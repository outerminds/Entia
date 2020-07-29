using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node With(this Node node, uint identifier) =>
            new Node(identifier, node.Kind, node.Tag, node.Value, node.Children);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node With(this Node node, params Node[] children) =>
            new Node(node.Identifier, node.Kind, node.Tag, node.Value, children);

        public static Node Map(this Node node, Func<Node, Node> map)
        {
            if (node.Children.Length > 0) node.With(node.Children.Select(map));
            return node;
        }

        public static Node Map<TState>(this Node node, in TState state, Func<Node, TState, Node> map)
        {
            if (node.Children.Length > 0) node.With(node.Children.Select(state, map));
            return node;
        }

        public static Node Add(this Node node, Node child) => node.With(node.Children.Append(child));
        public static Node Add(this Node node, params Node[] children) => node.With(node.Children.Append(children));
        public static Node AddAt(this Node node, int index, Node child) => node.With(node.Children.Insert(index, child));
        public static Node AddAt(this Node node, int index, params Node[] children) => node.With(node.Children.Insert(index, children));

        public static Node Remove(this Node node, Node child) => node.With(node.Children.Remove(child));
        public static Node Remove(this Node node, params Node[] children) => node.With(node.Children.Except(children).ToArray());
        public static Node RemoveAt(this Node node, int index) => node.With(node.Children.RemoveAt(index));
        public static Node RemoveAt(this Node node, int index, int count) => node.With(node.Children.RemoveAt(index, count));

        public static Node Remove(this Node node, Func<Node, bool> match)
        {
            if (node.Children.Length > 0)
            {
                var children = new List<Node>(node.Children.Length);
                foreach (var child in node.Children)
                {
                    if (match(child)) continue;
                    children.Add(child);
                }
                return node.With(children.ToArray());
            }
            else return node;
        }

        public static Node Replace(this Node node, Node child, Node replacement) =>
            node.ReplaceAt(Array.IndexOf(node.Children, child), replacement);
        public static Node ReplaceAt(this Node node, int index, Node replacement)
        {
            if (index < 0 || index >= node.Children.Length) return node;
            var children = node.Children.Clone() as Node[];
            children[index] = replacement;
            return node.With(children);
        }

        public static IEnumerable<Node> Family(this Node node)
        {
            yield return node;
            foreach (var descendant in node.Descendants()) yield return descendant;
        }

        public static IEnumerable<Node> Descendants(this Node node)
        {
            foreach (var child in node.Children)
            {
                yield return child;
                foreach (var descendant in child.Descendants()) yield return descendant;
            }
        }

        public static Node AddMember(this Node node, string key, Node value) =>
            node.TryMember(key, out _, out var index) ? node.ReplaceAt(index, value) : node.Add(Node.String(key), value);
        public static Node RemoveMember(this Node node, string key) =>
            node.TryMember(key, out _, out var index) ? node.RemoveAt(index, 2) : node;

        public static Node RemoveMembers(this Node node, Func<string, Node, bool> match)
        {
            if (node.IsObject())
            {
                var children = new List<Node>(node.Children.Length);
                foreach (var (key, value) in node.Members())
                {
                    if (match(key, value)) continue;
                    children.Add(key);
                    children.Add(value);
                }
                return node.With(children.ToArray());
            }
            else return node;
        }

        public static MemberEnumerable Members(this Node node) => new MemberEnumerable(node);
        public static bool HasMember(this Node node, string key) => node.TryMember(key, out _, out _);
        public static bool TryMember(this Node node, string key, out Node value) => node.TryMember(key, out value, out _);
        public static bool TryMember(this Node node, string key, out Node value, out int index)
        {
            if (node.IsObject())
            {
                for (index = 0; index < node.Children.Length; index += 2)
                {
                    if (node.Children[index].AsString() == key)
                    {
                        value = node.Children[index + 1];
                        return true;
                    }
                }
            }

            value = default;
            index = default;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is(this Node node, Node.Kinds kind) => node.Kind == kind;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has(this Node node, Node.Tags tags) => (node.Tag & tags) == tags;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasPlain(this Node node) => node.Has(Node.Tags.Plain);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasInteger(this Node node) => node.Has(Node.Tags.Integer);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasRational(this Node node) => node.Has(Node.Tags.Rational);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasEmpty(this Node node) => node.Has(Node.Tags.Empty);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasDollar(this Node node) => node.Has(Node.Tags.Dollar);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull(this Node node) => node.Is(Node.Kinds.Null);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsString(this Node node) => node.Is(Node.Kinds.String);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBoolean(this Node node) => node.Is(Node.Kinds.Boolean);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumber(this Node node) => node.Is(Node.Kinds.Number);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsArray(this Node node) => node.Is(Node.Kinds.Array);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsObject(this Node node) => node.Is(Node.Kinds.Object) && node.Children.Length % 2 == 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsType(this Node node) => node.Is(Node.Kinds.Type);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReference(this Node node) => node.Is(Node.Kinds.Reference);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAbstract(this Node node) => node.Is(Node.Kinds.Abstract) && node.Children.Length == 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryChar(this Node node, out char value)
        {
            if (node.IsNumber())
            {
                value =
                    node.HasInteger() ? (char)(long)node.Value :
                    node.HasRational() ? (char)(double)node.Value :
                    Convert.ToChar(node.Value, CultureInfo.InvariantCulture);
                return true;
            }
            else if (node.TryString(out var @string))
                return @string.TryFirst(out value);
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TrySByte(this Node node, out sbyte value)
        {
            if (node.IsNumber())
            {
                value =
                    node.HasInteger() ? (sbyte)(long)node.Value :
                    node.HasRational() ? (sbyte)(double)node.Value :
                    Convert.ToSByte(node.Value, CultureInfo.InvariantCulture);
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryByte(this Node node, out byte value)
        {
            if (node.IsNumber())
            {
                value =
                    node.HasInteger() ? (byte)(long)node.Value :
                    node.HasRational() ? (byte)(double)node.Value :
                    Convert.ToByte(node.Value, CultureInfo.InvariantCulture);
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryShort(this Node node, out short value)
        {
            if (node.IsNumber())
            {
                value =
                    node.HasInteger() ? (short)(long)node.Value :
                    node.HasRational() ? (short)(double)node.Value :
                    Convert.ToInt16(node.Value, CultureInfo.InvariantCulture);
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryUShort(this Node node, out ushort value)
        {
            if (node.IsNumber())
            {
                value =
                    node.HasInteger() ? (ushort)(long)node.Value :
                    node.HasRational() ? (ushort)(double)node.Value :
                    Convert.ToUInt16(node.Value, CultureInfo.InvariantCulture);
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryInt(this Node node, out int value)
        {
            if (node.IsNumber())
            {
                value =
                    node.HasInteger() ? (int)(long)node.Value :
                    node.HasRational() ? (int)(double)node.Value :
                    Convert.ToInt32(node.Value, CultureInfo.InvariantCulture);
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryUInt(this Node node, out uint value)
        {
            if (node.IsNumber())
            {
                value =
                    node.HasInteger() ? (uint)(long)node.Value :
                    node.HasRational() ? (uint)(double)node.Value :
                    Convert.ToUInt32(node.Value, CultureInfo.InvariantCulture);
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryLong(this Node node, out long value)
        {
            if (node.IsNumber())
            {
                value =
                    node.HasInteger() ? (long)node.Value :
                    node.HasRational() ? (long)(double)node.Value :
                    Convert.ToInt64(node.Value, CultureInfo.InvariantCulture);
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryULong(this Node node, out ulong value)
        {
            if (node.IsNumber())
            {
                value =
                    node.HasInteger() ? (ulong)(long)node.Value :
                    node.HasRational() ? (ulong)(double)node.Value :
                    Convert.ToUInt64(node.Value, CultureInfo.InvariantCulture);
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFloat(this Node node, out float value)
        {
            if (node.IsNumber())
            {
                value =
                    node.HasInteger() ? (float)(long)node.Value :
                    node.HasRational() ? (float)(double)node.Value :
                    Convert.ToSingle(node.Value, CultureInfo.InvariantCulture);
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryDouble(this Node node, out double value)
        {
            if (node.IsNumber())
            {
                value =
                    node.HasInteger() ? (double)(long)node.Value :
                    node.HasRational() ? (double)node.Value :
                    Convert.ToDouble(node.Value, CultureInfo.InvariantCulture);
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryDecimal(this Node node, out decimal value)
        {
            if (node.IsNumber())
            {
                value =
                    node.HasInteger() ? (decimal)(long)node.Value :
                    node.HasRational() ? (decimal)(double)node.Value :
                    Convert.ToDecimal(node.Value, CultureInfo.InvariantCulture);
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnum<T>(this Node node, out T value) where T : struct, Enum
        {
            if (node.IsNumber())
            {
                value =
                    node.HasInteger() ? (T)Enum.ToObject(typeof(T), (long)node.Value) :
                    (T)Enum.ToObject(typeof(T), node.Value);
                return true;
            }
            else if (node.TryString(out var @string))
                return Enum.TryParse(@string, out value);
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnum(this Node node, Type type, out Enum value)
        {
            if (node.IsNumber())
            {
                value =
                    node.HasInteger() ? (Enum)Enum.ToObject(type, (long)node.Value) :
                    (Enum)Enum.ToObject(type, node.Value);
                return true;
            }
            else if (node.TryString(out var @string))
            {
                try
                {
                    value = (Enum)Enum.Parse(type, @string);
                    return true;
                }
                catch { }
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryType(this Node node, out Type value)
        {
            if (node.IsType())
            {
                value = (Type)node.Value;
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReference(this Node node, out uint value)
        {
            if (node.IsReference())
            {
                value = (uint)node.Value;
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAbstract(this Node node, out Type type, out Node value)
        {
            if (node.IsAbstract() && node.Children[0].TryType(out type))
            {
                value = node.Children[1];
                return true;
            }
            type = default;
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AsString(this Node node, string @default = default) => node.TryString(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AsBool(this Node node, bool @default = default) => node.TryBool(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char AsChar(this Node node, char @default = default) => node.TryChar(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte AsSByte(this Node node, sbyte @default = default) => node.TrySByte(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte AsByte(this Node node, byte @default = default) => node.TryByte(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short AsShort(this Node node, short @default = default) => node.TryShort(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort AsUShort(this Node node, ushort @default = default) => node.TryUShort(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AsInt(this Node node, int @default = default) => node.TryInt(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint AsUInt(this Node node, uint @default = default) => node.TryUInt(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long AsLong(this Node node, long @default = default) => node.TryLong(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong AsULong(this Node node, ulong @default = default) => node.TryULong(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AsFloat(this Node node, float @default = default) => node.TryFloat(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AsDouble(this Node node, double @default = default) => node.TryDouble(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal AsDecimal(this Node node, decimal @default = default) => node.TryDecimal(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AsEnum<T>(this Node node, T @default = default) where T : struct, Enum => node.TryEnum<T>(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Enum AsEnum(this Node node, Type type, Enum @default = default) => node.TryEnum(type, out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type AsType(this Node node, Type @default = default) => node.TryType(out var value) ? value : @default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint AsReference(this Node node, uint @default = default) => node.TryReference(out var value) ? value : @default;
    }
}