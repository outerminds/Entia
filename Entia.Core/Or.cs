using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Entia.Core
{
    public interface IOr
    {
        Or.Tags Tag { get; }
    }

    public readonly struct Left<T> : IOr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Left<T>(in T value) => new Left<T>(value);

        public readonly T Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Left(T value) { Value = value; }

        Or.Tags IOr.Tag => Or.Tags.Left;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Or<T, TRight> AsOr<TRight>() => this;
        public override string ToString() => $"{GetType().Format()}({Value})";
        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Value);
    }

    public readonly struct Right<T> : IOr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Right<T>(in T value) => new Right<T>(value);

        public readonly T Value;
        Or.Tags IOr.Tag => Or.Tags.Right;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Right(T value) { Value = value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Or<TLeft, T> AsOr<TLeft>() => this;
        public override string ToString() => $"{GetType().Format()}({Value})";
        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Value);
    }

    public readonly struct Or<TLeft, TRight> : IOr, IEquatable<Or<TLeft, TRight>>, IEquatable<Left<TLeft>>, IEquatable<Right<TRight>>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Or<TLeft, TRight> or, in TLeft left) =>
            or.TryLeft(out var value) && EqualityComparer<TLeft>.Default.Equals(value, left);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Or<TLeft, TRight> or, in TRight right) =>
            or.TryRight(out var value) && EqualityComparer<TRight>.Default.Equals(value, right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Or<TLeft, TRight> left, in Or<TLeft, TRight> right) =>
            (left.TryLeft(out var leftValue) && right == leftValue) ||
            (left.TryRight(out var rightValue) && right == rightValue);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in TLeft left, in Or<TLeft, TRight> or) => or == left;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in TRight right, in Or<TLeft, TRight> or) => or == right;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Or<TLeft, TRight> or, in TLeft left) => !(or == left);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Or<TLeft, TRight> or, in TRight right) => !(or == right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Or<TLeft, TRight> left, in Or<TLeft, TRight> right) => !(left == right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in TLeft left, in Or<TLeft, TRight> or) => !(left == or);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in TRight right, in Or<TLeft, TRight> or) => !(right == or);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Or<TLeft, TRight>(in Left<TLeft> left) => left.Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Or<TLeft, TRight>(in Right<TRight> right) => right.Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Or<TLeft, TRight>(in TLeft value) => new Or<TLeft, TRight>(Or.Tags.Left, value, default);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Or<TLeft, TRight>(in TRight value) => new Or<TLeft, TRight>(Or.Tags.Right, default, value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator TLeft(in Or<TLeft, TRight> or) => or.IsLeft() ? or._left : throw new InvalidCastException();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator TRight(in Or<TLeft, TRight> or) => or.IsRight() ? or._right : throw new InvalidCastException();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Left<TLeft>(in Or<TLeft, TRight> or) => (TLeft)or;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Right<TRight>(in Or<TLeft, TRight> or) => (TRight)or;

        public Or.Tags Tag { get; }
        public Option<TLeft> Left => TryLeft(out var value) ? Option.From(value) : Option.None();
        public Option<TRight> Right => TryRight(out var value) ? Option.From(value) : Option.None();
        readonly TLeft _left;
        readonly TRight _right;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Or(Or.Tags tag, in TLeft left, in TRight right)
        {
            Tag = tag;
            _left = left;
            _right = right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLeft(out TLeft value)
        {
            value = _left;
            return Tag == Or.Tags.Left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRight(out TRight value)
        {
            value = _right;
            return Tag == Or.Tags.Right;
        }

        public override int GetHashCode() => Tag switch
        {
            Or.Tags.Left => Or.Left(_left).GetHashCode(),
            Or.Tags.Right => Or.Right(_right).GetHashCode(),
            _ => throw new InvalidOperationException()
        };

        public override string ToString() => Tag switch
        {
            Or.Tags.Left => Or.Left(_left).ToString(),
            Or.Tags.Right => Or.Right(_right).ToString(),
            _ => throw new InvalidOperationException()
        };

        public bool Equals(Or<TLeft, TRight> other) => this == other;
        public bool Equals(Left<TLeft> other) => this == other;
        public bool Equals(Right<TRight> other) => this == other;
        public override bool Equals(object obj) =>
            obj is TLeft leftValue ? this == leftValue :
            obj is TRight rightValue ? this == rightValue :
            obj is Left<TLeft> left ? this == left :
            obj is Right<TRight> right && this == right;
    }

    public static class Or
    {
        public enum Tags { None, Left, Right }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<TLeft, TRight>(in this Or<TLeft, TRight> or, Tags tag) => or.Tag == tag;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeft<TLeft, TRight>(in this Or<TLeft, TRight> or) => or.Is(Tags.Left);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRight<TLeft, TRight>(in this Or<TLeft, TRight> or) => or.Is(Tags.Right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Left<T> Left<T>(in T value) => value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Right<T> Right<T>(in T value) => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TRight, TLeft> Flip<TLeft, TRight>(in this Or<TLeft, TRight> or) =>
            or.Match(left => Right(left).AsOr<TRight>(), right => Left(right));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Match<TLeft, TRight, TOut>(in this Or<TLeft, TRight> or, Func<TLeft, TOut> left, Func<TRight, TOut> right) =>
            or.TryLeft(out var leftValue) ? left(leftValue) :
            or.TryRight(out var rightValue) ? right(rightValue) :
            throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Match<TLeft, TRight, TState, TOut>(in this Or<TLeft, TRight> or, in TState state, Func<TLeft, TState, TOut> left, Func<TRight, TState, TOut> right) =>
            or.TryLeft(out var leftValue) ? left(leftValue, state) :
            or.TryRight(out var rightValue) ? right(rightValue, state) :
            throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TTarget, TRight> MapLeft<TSource, TTarget, TRight, TState>(in this Or<TSource, TRight> or, in TState state, Func<TSource, TState, TTarget> map) =>
            or.Match((map, state), (value, state) => Left(state.map(value, state.state)).AsOr<TRight>(), (value, _) => Right(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TTarget, TRight> MapLeft<TSource, TTarget, TRight>(in this Or<TSource, TRight> or, Func<TSource, TTarget> map) =>
            or.Match(map, (value, state) => Left(state(value)).AsOr<TRight>(), (value, _) => Right(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, TTarget> MapRight<TLeft, TSource, TTarget, TState>(in this Or<TLeft, TSource> or, in TState state, Func<TSource, TState, TTarget> map) =>
            or.Match((map, state), (value, _) => Left(value).AsOr<TTarget>(), (value, state) => Right(state.map(value, state.state)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, TTarget> MapRight<TLeft, TSource, TTarget>(in this Or<TLeft, TSource> or, Func<TSource, TTarget> map) =>
            or.Match(map, (value, _) => Left(value).AsOr<TTarget>(), (value, state) => Right(state(value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, TRight> DoLeft<TLeft, TRight, TState>(in this Or<TLeft, TRight> or, in TState state, Action<TLeft, TState> @do) =>
            or.MapLeft((@do, state), (value, state) => { state.@do(value, state.state); return value; });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, TRight> DoLeft<TLeft, TRight>(in this Or<TLeft, TRight> or, Action<TLeft> @do) =>
            or.MapLeft(@do, (value, state) => { state(value); return value; });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, TRight> DoRight<TLeft, TRight, TState>(in this Or<TLeft, TRight> or, in TState state, Action<TRight, TState> @do) =>
            or.MapRight((@do, state), (value, state) => { state.@do(value, state.state); return value; });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, TRight> DoRight<TLeft, TRight>(in this Or<TLeft, TRight> or, Action<TRight> @do) =>
            or.MapRight(@do, (value, state) => { state(value); return value; });

        public static IEnumerable<TLeft> Lefts<TLeft, TRight>(this IEnumerable<Or<TLeft, TRight>> ors)
        {
            foreach (var or in ors) if (or.TryLeft(out var value)) yield return value;
        }

        public static IEnumerable<TRight> Rights<TLeft, TRight>(this IEnumerable<Or<TLeft, TRight>> ors)
        {
            foreach (var or in ors) if (or.TryRight(out var value)) yield return value;
        }
    }
}
