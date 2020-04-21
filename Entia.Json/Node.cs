using System;

namespace Entia.Json
{
    public sealed class Node
    {
        public enum Kinds : byte { Null, Boolean, Number, String, Object, Array }
        public enum Tags : byte { None, Plain = 1 << 0 }

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

        public static readonly Node Null = new Node(Kinds.Null, Tags.None, null);
        public static readonly Node True = new Node(Kinds.Boolean, Tags.None, true);
        public static readonly Node False = new Node(Kinds.Boolean, Tags.None, false);
        public static readonly Node Zero = new Node(Kinds.Number, Tags.None, 0L);

        static readonly Node[] _empty = { };

        public static Node Boolean(bool value) => value ? True : False;

        public static Node Number(char value) => Number((long)value);
        public static Node Number(byte value) => Number((long)value);
        public static Node Number(sbyte value) => Number((long)value);
        public static Node Number(short value) => Number((long)value);
        public static Node Number(ushort value) => Number((long)value);
        public static Node Number(int value) => Number((long)value);
        public static Node Number(uint value) => Number((long)value);
        public static Node Number(long value) => Number((object)value);
        public static Node Number(ulong value) => Number((long)value);
        public static Node Number(float value) => Number((double)value);
        public static Node Number(double value) => Number((object)value);
        public static Node Number(decimal value) => Number((double)value);
        internal static Node Number(object value) => new Node(Kinds.Number, Tags.None, value, _empty);

        public static Node String(char value) => String(value.ToString(), true);
        public static Node String(string value, bool plain = false) =>
            value is null ? Null :
            new Node(Kinds.String, plain ? Tags.Plain : Tags.None, value, _empty);

        public static Node Array(params Node[] items) => new Node(Kinds.Array, Tags.None, null, items);
        public static Node Object(params Node[] members) => new Node(Kinds.Object, Tags.None, null, members);
        public static Node Abstract(Node type, Node value) => Object("$t", type, "$v", value);
        public static Node Reference(int reference) => Object("$r", reference);

        public Node this[string key] => this.TryMember(key, out var value) ? value : throw new ArgumentException(nameof(key));
        public Node this[int index] => this.TryItem(index, out var item) ? item : throw new ArgumentException(nameof(index));

        public readonly Kinds Kind;
        public readonly Tags Tag;
        public readonly object Value;
        public readonly Node[] Children;

        public Node(Kinds kind, Tags tag, object value, params Node[] children)
        {
            Kind = kind;
            Tag = tag;
            Value = value;
            Children = children;
        }

        public override string ToString() => Serialization.Generate(this);
    }
}