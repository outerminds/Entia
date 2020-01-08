using System;

namespace Entia.Experiment.Json
{
    public sealed class Node
    {
        public enum Kinds { Null, Boolean, Number, String, Object, Member, Array }

        public static implicit operator Node(bool value) => Boolean(value);
        public static implicit operator Node(char value) => Number(value);
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
        public static implicit operator Node(string value) => String(value);

        public static readonly Node Null = new Node(Kinds.Null, "null");
        public static readonly Node True = new Node(Kinds.Boolean, bool.TrueString.ToLower());
        public static readonly Node False = new Node(Kinds.Boolean, bool.FalseString.ToLower());
        public static readonly Node Zero = new Node(Kinds.Number, "0");

        public static Node Boolean(bool value) => value ? True : False;
        public static Node Number(char value) => new Node(Kinds.Number, value.ToString());
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
        public static Node String(string value) => value is null ? Null : new Node(Kinds.String, value);
        public static Node Member(Node key, Node value) => new Node(Kinds.Member, key, value);
        public static Node Array(params Node[] items) => new Node(Kinds.Array, items);
        public static Node Object(params Node[] members) => new Node(Kinds.Object, members);
        public static Node Abstract(Node type, Node value) => Object(Member("$t", type), Member("$v", value));
        public static Node Reference(int reference) => Object(Member("$r", reference));

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
            Children = System.Array.Empty<Node>();
        }

        public override string ToString() =>
            this.TryMember(out var key, out var value) ? $"{key}, {value}" :
            string.IsNullOrEmpty(Value) ? $"{Kind}({Children.Length})" : $"{Kind}: {Value}";
    }

    public static class NodeExtensions
    {
        public static bool TryMember(this Node node, string member, out Node value)
        {
            if (node.Kind == Node.Kinds.Object)
            {
                foreach (var child in node.Children)
                    if (child.TryMember(member, out value)) return true;

                value = default;
                return false;
            }

            return node.TryMember(out var key, out value) && key.Value == member;
        }

        public static bool TryMember(this Node node, out Node key, out Node value)
        {
            if (node.Kind == Node.Kinds.Member && node.Children.Length == 2)
            {
                key = node.Children[0];
                value = node.Children[1];
                return true;
            }
            key = default;
            value = default;
            return false;
        }

        public static bool TryItem(this Node node, int index, out Node item)
        {
            if (node.Kind == Node.Kinds.Array && index < node.Children.Length)
            {
                item = node.Children[index];
                return true;
            }
            item = default;
            return false;
        }

        public static bool TryReference(this Node node, out Node reference)
        {
            if (node.Kind == Node.Kinds.Object && node.Children.Length == 1 &&
                node.Children[0].TryMember("$r", out reference))
                return true;
            reference = default;
            return false;
        }

        public static bool TryAbstract(this Node node, out Node type, out Node value)
        {
            if (node.Kind == Node.Kinds.Object && node.Children.Length == 2 &&
                node.Children[0].TryMember("$t", out type) &&
                node.Children[1].TryMember("$v", out value))
                return true;
            type = default;
            value = default;
            return false;
        }
    }
}