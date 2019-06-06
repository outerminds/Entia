using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public interface IBox
{
    Type Type { get; }
    object Value { get; set; }
}

public sealed class Box<T> : IBox, IEquatable<Box<T>>, IEquatable<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(Box<T> box) => box.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Box<T>(in T value) => new Box<T>(value);

    Type IBox.Type => typeof(T);
    object IBox.Value { get => Value; set => Value = (T)value; }

    public T Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box(in T value) { Value = value; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Box<T> other) => this == other || Equals(other.Value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(T other) => EqualityComparer<T>.Default.Equals(Value, other);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object obj) => obj is Box<T> box ? Equals(box) : obj is T value && Equals(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => Value?.ToString();
}
