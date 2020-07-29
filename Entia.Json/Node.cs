using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Entia.Json
{
    public sealed class Node
    {
        // NOTE: these enums are kept as 'byte' to keep the size of nodes small;
        // current size is 22 bytes on x64 (24 with padding) and should not go over 24 bytes since
        // it will significantly increase the size of parse trees
        public enum Kinds : byte { Null, Boolean, Number, String, Object, Array, Type, Reference, Abstract }
        public enum Tags : byte { None, Plain = 1 << 0, Integer = 1 << 1, Rational = 1 << 2, Empty = 1 << 3, Dollar = 1 << 4 }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(bool value) => Boolean(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(byte value) => Number(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(sbyte value) => Number(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(short value) => Number(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(ushort value) => Number(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(int value) => Number(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(uint value) => Number(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(long value) => Number(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(ulong value) => Number(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(float value) => Number(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(double value) => Number(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(decimal value) => Number(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(Enum value) => Number(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(char value) => Number(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(string value) => String(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Node(Type value) => Type(value);

        // NOTE: must be placed before calling the 'Node' constructors
        static readonly Node[] _empty = { };
        static int _counter;

        public static readonly Node Null = new Node(Kinds.Null, Tags.Empty, null, _empty);
        public static readonly Node True = new Node(Kinds.Boolean, Tags.None, true, _empty);
        public static readonly Node False = new Node(Kinds.Boolean, Tags.Empty, false, _empty);
        public static readonly Node ZeroInteger = new Node(Kinds.Number, Tags.Integer | Tags.Empty, 0L, _empty);
        public static readonly Node ZeroRational = new Node(Kinds.Number, Tags.Rational | Tags.Empty, 0d, _empty);
        public static readonly Node EmptyObject = new Node(Kinds.Object, Tags.Empty, null, _empty);
        public static readonly Node EmptyArray = new Node(Kinds.Array, Tags.Empty, null, _empty);
        public static readonly Node EmptyString = new Node(Kinds.String, Tags.Plain | Tags.Empty, "", _empty);
        public static readonly Node DollarString = new Node(Kinds.String, Tags.Plain | Tags.Dollar, "$", _empty);
        public static readonly Node DollarTString = new Node(Kinds.String, Tags.Plain | Tags.Dollar, "$t", _empty);
        public static readonly Node DollarIString = new Node(Kinds.String, Tags.Plain | Tags.Dollar, "$i", _empty);
        public static readonly Node DollarVString = new Node(Kinds.String, Tags.Plain | Tags.Dollar, "$v", _empty);
        public static readonly Node DollarRString = new Node(Kinds.String, Tags.Plain | Tags.Dollar, "$r", _empty);
        public static readonly Node DollarKString = new Node(Kinds.String, Tags.Plain | Tags.Dollar, "$k", _empty);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Boolean(bool value) => value ? True : False;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(char value) => Number((long)value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(byte value) => Number((long)value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(sbyte value) => Number((long)value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(short value) => Number((long)value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(ushort value) => Number((long)value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(int value) => Number((long)value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(uint value) => Number((long)value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(long value) => value == 0 ? ZeroInteger : new Node(Kinds.Number, Tags.Integer, value, _empty);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(ulong value) => value == 0 ? ZeroInteger : new Node(Kinds.Number, Tags.None, value, _empty);
        // NOTE: since conversion from 'float' to 'double' isn't perfect, it is best to allow to keep the
        // 'float' as is to prevent adding fractional digits
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(float value) => value == 0 ? ZeroRational : new Node(Kinds.Number, Tags.None, value, _empty);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(double value) => value == 0 ? ZeroRational : new Node(Kinds.Number, Tags.Rational, value, _empty);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(decimal value) => value == 0 ? ZeroRational : new Node(Kinds.Number, Tags.None, value, _empty);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(Enum value) => value == null ? Null : Number(Convert.ToInt64(value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node String(char value) => String(value.ToString(), Tags.None);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node String(Enum value) => value == null ? Null : String(value.ToString(), Tags.Plain);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node String(string value) => value == null ? Null : value.Length == 0 ? EmptyString : String(value, Tags.None);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Array(params Node[] items) => items.Length == 0 ? EmptyArray : Array(items, Tags.None);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Object(params Node[] members) => members.Length == 0 ? EmptyObject : Object(members, Tags.None);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Type(Type type) => type == null ? Null : new Node(Kinds.Type, Tags.None, type, _empty);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Abstract(Node type, Node value) => type == null ? Null : new Node(Kinds.Abstract, Tags.None, null, type, value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Reference(uint identifier) => new Node(Kinds.Reference, Tags.None, identifier, _empty);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Reserve() => (uint)Interlocked.Increment(ref _counter);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Node String(string value, Tags tags) => new Node(Kinds.String, tags, value, _empty);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Node Array(Node[] items, Tags tags) => new Node(Kinds.Array, tags, null, items);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Node Object(Node[] members, Tags tags) => new Node(Kinds.Object, tags, null, members);

        public Node this[string key] => this.TryMember(key, out var value) ? value : throw new ArgumentException(nameof(key));
        public Node this[int index] => this.TryItem(index, out var item) ? item : throw new ArgumentException(nameof(index));

        public readonly uint Identifier;
        public readonly Kinds Kind;
        public readonly Tags Tag;
        public readonly object Value;
        public readonly Node[] Children;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Node(Kinds kind, Tags tag, object value, params Node[] children) :
            this(Reserve(), kind, tag, value, children)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Node(uint identifier, Kinds kind, Tags tag, object value, params Node[] children)
        {
            Identifier = identifier;
            Kind = kind;
            Tag = tag;
            Value = value;
            Children = children;
        }

        public override string ToString() => Serialization.Generate(this);
    }
}