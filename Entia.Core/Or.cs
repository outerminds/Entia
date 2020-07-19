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
        Or.Tags IOr.Tag => Or.Tags.Left;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Left(T value) { Value = value; }
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

    public readonly struct Or<T1, T2> : IOr, IEquatable<Or<T1, T2>>, IEquatable<Left<T1>>, IEquatable<Right<T2>>
    {
        public static bool operator ==(in Or<T1, T2> or, in T1 left) =>
            or.TryLeft(out var value) && EqualityComparer<T1>.Default.Equals(value, left);
        public static bool operator ==(in Or<T1, T2> or, in T2 right) =>
            or.TryRight(out var value) && EqualityComparer<T2>.Default.Equals(value, right);
        public static bool operator ==(in Or<T1, T2> left, in Or<T1, T2> right) =>
            (left.TryLeft(out var leftValue) && right == leftValue) ||
            (left.TryRight(out var rightValue) && right == rightValue);
        public static bool operator ==(in T1 left, in Or<T1, T2> or) => or == left;
        public static bool operator ==(in T2 right, in Or<T1, T2> or) => or == right;
        public static bool operator !=(in Or<T1, T2> or, in T1 left) => !(or == left);
        public static bool operator !=(in Or<T1, T2> or, in T2 right) => !(or == right);
        public static bool operator !=(in Or<T1, T2> left, in Or<T1, T2> right) => !(left == right);
        public static bool operator !=(in T1 left, in Or<T1, T2> or) => !(left == or);
        public static bool operator !=(in T2 right, in Or<T1, T2> or) => !(right == or);
        public static implicit operator Or<T1, T2>(in Left<T1> left) => left.Value;
        public static implicit operator Or<T1, T2>(in Right<T2> right) => right.Value;
        public static implicit operator Or<T1, T2>(in T1 value) => new Or<T1, T2>(Or.Tags.Left, value, default);
        public static implicit operator Or<T1, T2>(in T2 value) => new Or<T1, T2>(Or.Tags.Right, default, value);
        public static explicit operator T1(in Or<T1, T2> or) => or.IsLeft() ? or._left : throw new InvalidCastException();
        public static explicit operator T2(in Or<T1, T2> or) => or.IsRight() ? or._right : throw new InvalidCastException();
        public static explicit operator Left<T1>(in Or<T1, T2> or) => (T1)or;
        public static explicit operator Right<T2>(in Or<T1, T2> or) => (T2)or;

        public Or.Tags Tag { get; }
        public Option<T1> Left => TryLeft(out var value) ? Option.From(value) : Option.None();
        public Option<T2> Right => TryRight(out var value) ? Option.From(value) : Option.None();
        readonly T1 _left;
        readonly T2 _right;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Or(Or.Tags tag, in T1 left, in T2 right)
        {
            Tag = tag;
            _left = left;
            _right = right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLeft(out T1 value)
        {
            value = _left;
            return Tag == Or.Tags.Left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRight(out T2 value)
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

        public bool Equals(Or<T1, T2> other) => this == other;
        public bool Equals(Left<T1> other) => this == other;
        public bool Equals(Right<T2> other) => this == other;
        public override bool Equals(object obj) =>
            obj is T1 leftValue ? this == leftValue :
            obj is T2 rightValue ? this == rightValue :
            obj is Left<T1> left ? this == left :
            obj is Right<T2> right && this == right;
    }

    public static class Or
    {
        public enum Tags { None, Left, Right }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T1, T2>(in this Or<T1, T2> or, Tags tag) => or.Tag == tag;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeft<T1, T2>(in this Or<T1, T2> or) => or.Is(Tags.Left);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRight<T1, T2>(in this Or<T1, T2> or) => or.Is(Tags.Right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Left<T> Left<T>(in T value) => value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Right<T> Right<T>(in T value) => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<T2, T1> Flip<T1, T2>(in this Or<T1, T2> or) =>
            or.Match(left => Right(left).AsOr<T2>(), right => Left(right));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<T, Unit> AsOr<T>(in this Option<T> option) =>
            option.Match(value => Left(value).AsOr<Unit>(), () => Right(default(Unit)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> AsOption<T>(in this Or<T, Unit> or) =>
            or.Match(value => Option.From(value), _ => Option.None());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<T, string[]> AsOr<T>(in this Result<T> result) =>
            result.Match(value => Left(value).AsOr<string[]>(), messages => Right(messages));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> AsResult<T>(in this Or<T, string[]> or) =>
            or.Match(value => Result.Success(value).AsResult(), messages => Result.Failure(messages));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Match<T1, T2, TOut>(in this Or<T1, T2> or, Func<T1, TOut> left, Func<T2, TOut> right) =>
            or.TryLeft(out var leftValue) ? left(leftValue) :
            or.TryRight(out var rightValue) ? right(rightValue) :
            throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Match<T1, T2, TState, TOut>(in this Or<T1, T2> or, in TState state, Func<T1, TState, TOut> left, Func<T2, TState, TOut> right) =>
            or.TryLeft(out var leftValue) ? left(leftValue, state) :
            or.TryRight(out var rightValue) ? right(rightValue, state) :
            throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TTarget, T> MapLeft<TSource, TTarget, T, TState>(in this Or<TSource, T> or, in TState state, Func<TSource, TState, TTarget> map) =>
            or.Match((map, state), (value, state) => Left(state.map(value, state.state)).AsOr<T>(), (value, _) => Right(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TTarget, T> MapLeft<TSource, TTarget, T>(in this Or<TSource, T> or, Func<TSource, TTarget> map) =>
            or.Match(map, (value, state) => Left(state(value)).AsOr<T>(), (value, _) => Right(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<T, TTarget> MapRight<T, TSource, TTarget, TState>(in this Or<T, TSource> or, in TState state, Func<TSource, TState, TTarget> map) =>
            or.Match((map, state), (value, _) => Left(value).AsOr<TTarget>(), (value, state) => Right(state.map(value, state.state)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<T, TTarget> MapRight<T, TSource, TTarget>(in this Or<T, TSource> or, Func<TSource, TTarget> map) =>
            or.Match(map, (value, _) => Left(value).AsOr<TTarget>(), (value, state) => Right(state(value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<T1, T2> DoLeft<T1, T2, TState>(in this Or<T1, T2> or, in TState state, Action<T1, TState> @do) =>
            or.MapLeft((@do, state), (value, state) => { state.@do(value, state.state); return value; });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<T1, T2> DoLeft<T1, T2>(in this Or<T1, T2> or, Action<T1> @do) =>
            or.MapLeft(@do, (value, state) => { state(value); return value; });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<T1, T2> DoRight<T1, T2, TState>(in this Or<T1, T2> or, in TState state, Action<T2, TState> @do) =>
            or.MapRight((@do, state), (value, state) => { state.@do(value, state.state); return value; });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<T1, T2> DoRight<T1, T2>(in this Or<T1, T2> or, Action<T2> @do) =>
            or.MapRight(@do, (value, state) => { state(value); return value; });

        public static IEnumerable<T1> Lefts<T1, T2>(this IEnumerable<Or<T1, T2>> ors)
        {
            foreach (var or in ors) if (or.TryLeft(out var value)) yield return value;
        }

        public static IEnumerable<T2> Rights<T1, T2>(this IEnumerable<Or<T1, T2>> ors)
        {
            foreach (var or in ors) if (or.TryRight(out var value)) yield return value;
        }
    }
}
