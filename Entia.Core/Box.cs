using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public interface IBox
{
    bool Valid { get; }
    Array Box { get; }
    Type Type { get; }
}

public readonly struct Box : IBox, IEquatable<Box>, IEquatable<Box.Read>
{
    public readonly struct Read : IBox, IEquatable<Box>, IEquatable<Box.Read>
    {
        public object Value => _box.GetValue(0);
        public Type Type => _box.GetType().GetElementType();
        public bool Valid => _box != null;
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

        public bool Equals(Read other) => this._box == other._box || Equals(other.Value);
        public bool Equals(Box other) => this._box == other._box || Equals(other.Value);
        public override bool Equals(object obj) =>
            obj is Box box ? Equals(box) :
            obj is Read read ? Equals(read) :
            Equals(Value, obj);
        public override int GetHashCode() => _box.GetHashCode();
        public override string ToString() => Value?.ToString();
    }

    public static implicit operator Read(Box box) => new Read(box._box);

    public object Value { get => _box.GetValue(0); set => _box.SetValue(value, 0); }
    public Type Type => _box.GetType().GetElementType();
    public bool Valid => _box != null;
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

    public bool Equals(Read other) => other.Equals(this);
    public bool Equals(Box other) => this._box == other._box || Equals(other.Value);
    public override bool Equals(object obj) =>
        obj is Box box ? Equals(box) :
        obj is Read read ? Equals(read) :
        Equals(Value, obj);
    public override int GetHashCode() => _box.GetHashCode();
    public override string ToString() => Value?.ToString();
}

public readonly struct Box<T> : IBox, IEquatable<Box<T>>, IEquatable<Box<T>.Read>, IEquatable<T>
{
    public readonly struct Read : IBox, IEquatable<Box<T>>, IEquatable<Read>, IEquatable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(Read box) => box.Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Read(in T value) => new Read(value);

        public ref readonly T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _box[0];
        }
        public bool Valid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _box != null;
        }
        Type IBox.Type => typeof(T);
        Array IBox.Box => _box;

        readonly T[] _box;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Read(in T value) : this(new T[] { value }) { }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Read(T[] box) { _box = box; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Read other) => this._box == other._box || Equals(other.Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Box<T> other) => this._box == other._box || Equals(other.Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(T other) => EqualityComparer<T>.Default.Equals(Value, other);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) =>
            obj is Box<T> box ? Equals(box) :
            obj is Read read ? Equals(read) :
            obj is T value && Equals(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => _box.GetHashCode();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => Value?.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Box.Read(Box<T> box) => new Box.Read(box._box);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Box(Box<T> box) => new Box(box._box);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(Box<T> box) => box.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Box<T>(in T value) => new Box<T>(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Read(Box<T> box) => new Read(box._box);

    public ref T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _box[0];
    }
    public bool Valid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _box != null;
    }
    Type IBox.Type => typeof(T);
    Array IBox.Box => _box;

    readonly T[] _box;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box(in T value) : this(new T[] { value }) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box(T[] box) { _box = box; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Read other) => other.Equals(this);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Box<T> other) => this._box == other._box || Equals(other.Value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(T other) => EqualityComparer<T>.Default.Equals(Value, other);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object obj) =>
        obj is Box<T> box ? Equals(box) :
        obj is Read read ? Equals(read) :
        obj is T value && Equals(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => _box.GetHashCode();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => Value?.ToString();
}
