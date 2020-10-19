using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Entia.Core;

namespace Entia.Json
{
    /// <summary>
    /// Data structure that represents a json parse tree.
    /// <para>
    /// Many constructors, operators and extensions are provided for this type to make it easy
    /// to manipulate it.
    /// </para>
    /// </summary>
    public sealed class Node
    {
        // These enums are kept as 'byte' to keep the size of nodes small.
        // Current size is 22 bytes on x64 (24 with padding) and should not go over 24 bytes since
        // it will significantly increase the size of parse trees.
        public enum Kinds : byte
        {
            Null,
            Boolean,
            Number,
            String,
            Object,
            Array,
            Type,
            Reference,
            Abstract
        }

        public enum Tags : byte
        {
            None,
            Plain = 1 << 0,
            Integer = 1 << 1,
            Rational = 1 << 2,
            Empty = 1 << 3,
            Dollar = 1 << 4,
            Zero = 1 << 5
        }

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

        // Must be placed before calling the 'Node' constructors.
        static readonly Node[] _empty = { };
        static readonly Node[] _singles = new Node[128];
        static readonly Node[] _dollars = new Node[128];
        // It has been estimated to be very improbable that this counter would overflow and cause
        // identifier collisions within the same node tree.
        static int _counter;

        public static readonly Node Null = new Node(Kinds.Null, Tags.Empty, null, _empty);
        public static readonly Node True = new Node(Kinds.Boolean, Tags.None, true, _empty);
        public static readonly Node False = new Node(Kinds.Boolean, Tags.Zero, false, _empty);
        public static readonly Node Zero = new Node(Kinds.Number, Tags.Integer | Tags.Zero, 0L, _empty);
        public static readonly Node EmptyObject = new Node(Kinds.Object, Tags.Empty, null, _empty);
        public static readonly Node EmptyArray = new Node(Kinds.Array, Tags.Empty, null, _empty);
        public static readonly Node EmptyString = new Node(Kinds.String, Tags.Plain | Tags.Empty, "", _empty);

        internal static readonly Node DollarTString = Dollar('t');
        internal static readonly Node DollarIString = Dollar('i');
        internal static readonly Node DollarVString = Dollar('v');
        internal static readonly Node DollarRString = Dollar('r');
        internal static readonly Node DollarKString = Dollar('k');

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
        public static Node Number(long value) => value == 0 ? Zero : new Node(Kinds.Number, Tags.Integer, value, _empty);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(ulong value) => value < long.MaxValue ? Number((long)value) : new Node(Kinds.Number, Tags.None, value, _empty);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(float value)
        {
            // Since conversion from 'float' to 'double' isn't perfect, it is best to allow to keep the
            // 'float' as is to prevent adding fractional digits
            var integer = (long)value;
            return value == integer ? Number(integer) : new Node(Kinds.Number, Tags.None, value, _empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(double value)
        {
            var integer = (long)value;
            return value == integer ? Number(integer) : Rational(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(decimal value)
        {
            var integer = (long)value;
            return value == integer ? Number(integer) : new Node(Kinds.Number, Tags.None, value, _empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Number(Enum value) => value == null ? Null : Number(Convert.ToInt64(value, CultureInfo.InvariantCulture));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node String(char value)
        {
            if (value < _singles.Length)
                return _singles[value] ?? (_singles[value] = String(value.ToString(), GetTags(value)));
            else
                return String(value.ToString(), DefaultTags(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node String(Enum value) => value == null ? Null : String(value.ToString(), Tags.Plain);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node String(string value)
        {
            if (value == null) return Null;
            if (value.Length == 0) return EmptyString;
            if (value.Length == 1) return String(value[0]);
            if (value.Length == 2 && value[0] == '$') return Dollar(value[1]);
            return String(value, Tags.None);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Array(params Node[] items) => items.Length == 0 ? EmptyArray : new Node(Kinds.Array, Tags.None, null, items);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Object(params Node[] members) => members.Length == 0 ? EmptyObject : new Node(Kinds.Object, Tags.None, null, members);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Type(Type type) => type == null ? Null : new Node(Kinds.Type, Tags.None, type, _empty);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Abstract(Node type, Node value) => new Node(Kinds.Abstract, Tags.None, null, type, value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node Reference(uint identifier) => new Node(Kinds.Reference, Tags.None, identifier, _empty);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Reserve() => (uint)Interlocked.Increment(ref _counter);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Node String(string value, Tags tags) => new Node(Kinds.String, tags, value, _empty);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Node Rational(float value) => new Node(Kinds.Number, Tags.None, value, _empty);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Node Rational(double value) => new Node(Kinds.Number, Tags.Rational, value, _empty);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Node Rational(decimal value) => new Node(Kinds.Number, Tags.None, value, _empty);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Node Dollar(char value)
        {
            if (value < _dollars.Length)
                return _dollars[value] ?? (_dollars[value] = String("$" + value, GetTags(value) | Tags.Dollar));
            else
                return String("$" + value, Tags.Dollar | DefaultTags(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Tags GetTags(char value)
        {
            switch (value)
            {
                case '\n':
                case '\b':
                case '\f':
                case '\r':
                case '\t':
                case '"':
                case '\\': return Tags.None;
                case '$': return Tags.Dollar | Tags.Plain;
                case '\0': return Tags.Zero;
                default: return DefaultTags(value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Tags DefaultTags(char value) => value <= byte.MaxValue ? Tags.Plain : Tags.None;

        public Node this[string key] => this.TryMember(key, out var value) ? value : throw new ArgumentException(nameof(key));
        public Node this[int index] => this.TryItem(index, out var item) ? item : throw new ArgumentException(nameof(index));

        public readonly uint Identifier;
        public readonly Kinds Kind;
        public readonly Tags Tag;
        public readonly object Value;
        public readonly Node[] Children;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Node(Kinds kind, Tags tag, object value, params Node[] children) :
            this(Reserve(), kind, tag, value, children)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Node(uint identifier, Kinds kind, Tags tag, object value, params Node[] children)
        {
            Identifier = identifier;
            Kind = kind;
            Tag = tag;
            Value = value;
            Children = children;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Node With(params Node[] children) => new Node(Identifier, Kind, Tag, Value, children);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Node With(uint identifier) => new Node(identifier, Kind, Tag, Value, Children);

        public override string ToString() => Kind switch
        {
            Kinds.Null => "null",
            Kinds.Boolean => (bool)Value ? "true" : "false",
            Kinds.String => @$"""{Value}""",
            Kinds.Type => ((Type)Value).FullFormat(),
            Kinds.Array => $"[{string.Join(", ", Children.AsEnumerable())}]",
            Kinds.Object => $"{{{string.Join(", ", Children.Tuples().Select(tuple => $"{tuple.Item1}: {tuple.Item2}"))}}}",
            Kinds.Abstract => $"{Children[1]} ({Children[0]})",
            Kinds.Reference => $"${Value}",
            _ => Convert.ToString(Value, CultureInfo.InvariantCulture)
        };
    }
}