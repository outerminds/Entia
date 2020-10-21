using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Entia.Core
{
    /// <summary>
    /// Interface that allows to interact with an instance of <see cref="Or{TLeft, TRight}"/> abstractly.
    /// </summary>
    public interface IOr
    {
        Or.Tags Tag { get; }
        object Value { get; }
    }

    public readonly struct Left<T> : IOr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Left<T>(in T value) => new Left<T>(value);

        public readonly T Value;
        Or.Tags IOr.Tag => Or.Tags.Left;
        object IOr.Value => Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Left(in T value) { Value = value; }

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
        object IOr.Value => Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Right(in T value) { Value = value; }

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
        public Option<TLeft> Left => this.Match(left => Option.From(left), _ => Option.None());
        public Option<TRight> Right => this.Match(_ => Option.None(), right => Option.From(right));
        object IOr.Value => this.Match(left => (object)left, right => (object)right);
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

    /// <summary>
    /// Module that exposes many common <see cref="Or{TLeft, TRight}"/> constructors and utility functions.
    /// </summary>
    public static class Or
    {
        public enum Tags { None, Left, Right }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Left<T> Left<T>(in T value) => value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Right<T> Right<T>(in T value) => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(this T or, Tags tag) where T : IOr => or.Tag == tag;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<TLeft, TRight>(in this Or<TLeft, TRight> or, Tags tag) => or.Tag == tag;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeft<T>(this T or) where T : IOr => or.Is(Tags.Left);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeft<TLeft, TRight>(in this Or<TLeft, TRight> or) => or.Is(Tags.Left);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRight<T>(this T or) where T : IOr => or.Is(Tags.Right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRight<TLeft, TRight>(in this Or<TLeft, TRight> or) => or.Is(Tags.Right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TRight, TLeft> Flip<TLeft, TRight>(in this Or<TLeft, TRight> or) =>
            or.Match(left => Right(left).AsOr<TRight>(), right => Left(right));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Match<TLeft, TRight, TOut>(in this Or<TLeft, TRight> or, Func<TLeft, TOut> matchLeft, Func<TRight, TOut> matchRight) =>
            or.TryLeft(out var left) ? matchLeft(left) :
            or.TryRight(out var right) ? matchRight(right) :
            throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Match<TLeft, TRight, TState, TOut>(in this Or<TLeft, TRight> or, in TState state, Func<TLeft, TState, TOut> matchLeft, Func<TRight, TState, TOut> matchRight) =>
            or.TryLeft(out var left) ? matchLeft(left, state) :
            or.TryRight(out var right) ? matchRight(right, state) :
            throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TTargetLeft, TTargetRight> Map<TSourceLeft, TTargetLeft, TSourceRight, TTargetRight, TState>(in this Or<TSourceLeft, TSourceRight> or, in TState state, Func<TSourceLeft, TState, TTargetLeft> mapLeft, Func<TSourceRight, TState, TTargetRight> mapRight) =>
            or.TryLeft(out var left) ? Left(mapLeft(left, state)).AsOr<TTargetRight>() :
            or.TryRight(out var right) ? Right(mapRight(right, state)).AsOr<TTargetLeft>() :
            throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TTargetLeft, TTargetRight> Map<TSourceLeft, TTargetLeft, TSourceRight, TTargetRight>(in this Or<TSourceLeft, TSourceRight> or, Func<TSourceLeft, TTargetLeft> mapLeft, Func<TSourceRight, TTargetRight> mapRight) =>
            or.TryLeft(out var left) ? Left(mapLeft(left)).AsOr<TTargetRight>() :
            or.TryRight(out var right) ? Right(mapRight(right)).AsOr<TTargetLeft>() :
            throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TTarget, TRight> MapLeft<TSource, TTarget, TRight, TState>(in this Or<TSource, TRight> or, in TState state, Func<TSource, TState, TTarget> map) =>
            or.Map(state, map, (value, _) => value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TTarget, TRight> MapLeft<TSource, TTarget, TRight>(in this Or<TSource, TRight> or, Func<TSource, TTarget> map) =>
            or.Map(map, value => value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, TTarget> MapRight<TLeft, TSource, TTarget, TState>(in this Or<TLeft, TSource> or, in TState state, Func<TSource, TState, TTarget> map) =>
            or.Map(state, (value, _) => value, map);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, TTarget> MapRight<TLeft, TSource, TTarget>(in this Or<TLeft, TSource> or, Func<TSource, TTarget> map) =>
            or.Map(value => value, map);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TLeft LeftOr<TLeft, TRight>(in this Or<TLeft, TRight> or, in TLeft value) =>
            or.TryLeft(out var left) ? left : value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TLeft LeftOr<TLeft, TRight, TState>(in this Or<TLeft, TRight> or, in TState state, Func<TRight, TState, TLeft> provide) =>
            or.Match(state, (left, _) => left, provide);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TLeft LeftOr<TLeft, TRight>(in this Or<TLeft, TRight> or, Func<TRight, TLeft> provide) =>
            or.Match(left => left, provide);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRight RightOr<TLeft, TRight>(in this Or<TLeft, TRight> or, in TRight value) =>
            or.TryRight(out var right) ? right : value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRight RightOr<TLeft, TRight, TState>(in this Or<TLeft, TRight> or, in TState state, Func<TLeft, TState, TRight> provide) =>
            or.Match(state, provide, (right, _) => right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TRight RightOr<TLeft, TRight>(in this Or<TLeft, TRight> or, Func<TLeft, TRight> provide) =>
            or.Match(provide, right => right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, TRight> Do<TLeft, TRight, TState>(in this Or<TLeft, TRight> or, in TState state, Action<TLeft, TState> doLeft, Action<TRight, TState> doRight)
        {
            if (or.TryLeft(out var left)) doLeft(left, state);
            else if (or.TryRight(out var right)) doRight(right, state);
            else throw new InvalidOperationException();
            return or;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, TRight> Do<TLeft, TRight>(in this Or<TLeft, TRight> or, Action<TLeft> doLeft, Action<TRight> doRight)
        {
            if (or.TryLeft(out var left)) doLeft(left);
            else if (or.TryRight(out var right)) doRight(right);
            else throw new InvalidOperationException();
            return or;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, TRight> DoLeft<TLeft, TRight, TState>(in this Or<TLeft, TRight> or, in TState state, Action<TLeft, TState> @do) =>
            or.Do(state, @do, (_, __) => { });
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, TRight> DoLeft<TLeft, TRight>(in this Or<TLeft, TRight> or, Action<TLeft> @do) =>
            or.Do(@do, _ => { });
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, TRight> DoRight<TLeft, TRight, TState>(in this Or<TLeft, TRight> or, in TState state, Action<TRight, TState> @do) =>
            or.Do(state, (_, __) => { }, @do);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, TRight> DoRight<TLeft, TRight>(in this Or<TLeft, TRight> or, Action<TRight> @do) =>
            or.Do(_ => { }, @do);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<Unit, Unit> Ignore<TLeft, TRight>(in this Or<TLeft, TRight> or) =>
            or.Map(_ => default(Unit), _ => default(Unit));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<Unit, TRight> IgnoreLeft<TLeft, TRight>(in this Or<TLeft, TRight> or) =>
            or.MapLeft(_ => default(Unit));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<TLeft, Unit> IgnoreRight<TLeft, TRight>(in this Or<TLeft, TRight> or) =>
            or.MapRight(_ => default(Unit));

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
