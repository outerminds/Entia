using System;
using System.Collections.Generic;

namespace Entia.Core
{
    public interface IBox
    {
        bool IsValid { get; }
        Array Box { get; }
        Type Type { get; }
    }

    public readonly struct Box : IBox, IEquatable<Box>, IEquatable<Box.Read>
    {
        public readonly struct Read : IBox, IEquatable<Box>, IEquatable<Box.Read>
        {
            public object Value => _box.GetValue(0);
            public Type Type => _box.GetType().GetElementType();
            public bool IsValid => _box?.Length > 0;
            Array IBox.Box => _box;

            readonly Array _box;
            public Read(object value, Type type) : this(Array.CreateInstance(type, 1)) { _box.SetValue(value, 0); }
            public Read(Array box) { _box = box; }

            public bool TryAs<T>(out Box<T>.Read box)
            {
                if (_box is T[] casted)
                {
                    box = new Box<T>.Read(casted);
                    return true;
                }

                box = default;
                return false;
            }

            public bool TryValue(out object value)
            {
                if (IsValid)
                {
                    value = Value;
                    return true;
                }
                value = default;
                return false;
            }

            public bool Equals(Read other) => this._box == other._box || (TryValue(out var value) && other.Equals(value));
            public bool Equals(Box other) => this._box == other._box || (TryValue(out var value) && other.Equals(value));
            public override bool Equals(object obj) =>
                obj is Box box ? Equals(box) :
                obj is Read read ? Equals(read) :
                TryValue(out var value) & Equals(value, obj);
            public override int GetHashCode() => _box?.GetHashCode() ?? 0;
            public override string ToString() => TryValue(out var value) ? value?.ToString() : "";
        }

        public static implicit operator Read(Box box) => new Read(box._box);

        public object Value { get => _box.GetValue(0); set => _box.SetValue(value, 0); }
        public Type Type => _box.GetType().GetElementType();
        public bool IsValid => _box?.Length > 0;
        Array IBox.Box => _box;

        readonly Array _box;
        public Box(object value, Type type) : this(Array.CreateInstance(type, 1)) { _box.SetValue(value, 0); }
        public Box(Array box) { _box = box; }

        public bool TryAs<T>(out Box<T> box)
        {
            if (_box is T[] casted)
            {
                box = new Box<T>(casted);
                return true;
            }

            box = default;
            return false;
        }

        public bool TryValue(out object value)
        {
            if (IsValid)
            {
                value = Value;
                return true;
            }
            value = default;
            return false;
        }

        public bool Equals(Read other) => EqualityComparer<Read>.Default.Equals(this, other);
        public bool Equals(Box other) => EqualityComparer<Read>.Default.Equals(this, other);
        public override bool Equals(object obj) =>
            obj is Box box ? Equals(box) :
            obj is Read read ? Equals(read) :
            TryValue(out var value) & Equals(value, obj);
        public override int GetHashCode() => _box?.GetHashCode() ?? 0;
        public override string ToString() => TryValue(out var value) ? value?.ToString() : "";
    }

    public readonly struct Box<T> : IBox, IEquatable<Box<T>>, IEquatable<Box<T>.Read>, IEquatable<T>
    {
        public readonly struct Read : IBox, IEquatable<Box<T>>, IEquatable<Read>, IEquatable<T>
        {
            public static implicit operator Read(in T value) => new Read(value);

            public ref readonly T Value => ref _box[0];
            public bool IsValid => _box?.Length > 0;
            Type IBox.Type => typeof(T);
            Array IBox.Box => _box;

            readonly T[] _box;

            public Read(in T value) : this(new T[] { value }) { }
            public Read(T[] box) { _box = box; }

            public bool TryValue(out T value)
            {
                if (IsValid)
                {
                    value = Value;
                    return true;
                }
                value = default;
                return false;
            }

            public bool Equals(Read other) => this._box == other._box || (TryValue(out var value) && other.Equals(value));
            public bool Equals(Box<T> other) => this._box == other._box || (TryValue(out var value) && other.Equals(value));
            public bool Equals(T other) => TryValue(out var value) & EqualityComparer<T>.Default.Equals(value, other);
            public override bool Equals(object obj) =>
                obj is Box<T> box ? Equals(box) :
                obj is Read read ? Equals(read) :
                obj is T value && Equals(value);
            public override int GetHashCode() => _box?.GetHashCode() ?? 0;
            public override string ToString() => TryValue(out var value) ? value?.ToString() : "";
        }

        public static implicit operator Box.Read(Box<T> box) => new Box.Read(box._box);
        public static implicit operator Box(Box<T> box) => new Box(box._box);
        public static implicit operator Box<T>(in T value) => new Box<T>(value);
        public static implicit operator Read(Box<T> box) => new Read(box._box);

        public ref T Value => ref _box[0];
        public bool IsValid => _box?.Length > 0;
        Type IBox.Type => typeof(T);
        Array IBox.Box => _box;

        readonly T[] _box;

        public Box(in T value) : this(new T[] { value }) { }
        public Box(T[] box) { _box = box; }

        public bool TryValue(out T value)
        {
            if (IsValid)
            {
                value = Value;
                return true;
            }
            value = default;
            return false;
        }

        public bool Equals(Read other) => EqualityComparer<Read>.Default.Equals(this, other);
        public bool Equals(Box<T> other) => EqualityComparer<Read>.Default.Equals(this, other);
        public bool Equals(T other) => TryValue(out var value) & EqualityComparer<T>.Default.Equals(value, other);
        public override bool Equals(object obj) =>
            obj is Box<T> box ? Equals(box) :
            obj is Read read ? Equals(read) :
            obj is T value && Equals(value);
        public override int GetHashCode() => _box?.GetHashCode() ?? 0;
        public override string ToString() => TryValue(out var value) ? value?.ToString() : "";
    }
}