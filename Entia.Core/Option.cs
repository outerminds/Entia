using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Entia.Core
{
    public interface IOption
    {
        Option.Tags Tag { get; }
        object Value { get; }
        Option<T> Cast<T>();
    }

    public readonly struct None : IOption
    {
        Option.Tags IOption.Tag => Option.Tags.None;
        object IOption.Value => null;
        Option<T> IOption.Cast<T>() => this;
        public override int GetHashCode() => 0;
        public override string ToString() => nameof(Option.Tags.None);
    }

    public readonly struct Option<T> : IOption, IEquatable<Option<T>>, IEquatable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Option<T>(in T value) => new Option<T>(value == null ? Option.Tags.None : Option.Tags.Some, value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Option<T>(None _) => new Option<T>(Option.Tags.None, default);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(in Option<T> option) => option.IsSome();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Option<T> left, in T right) => left.TryValue(out var value) && EqualityComparer<T>.Default.Equals(value, right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Option<T> left, in T right) => !(left == right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in T left, in Option<T> right) => right == left;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in T left, in Option<T> right) => !(left == right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Option<T> left, in Option<T> right) => left.TryValue(out var value) ? right == value : right.IsNone();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Option<T> left, in Option<T> right) => !(left == right);

        public Option.Tags Tag { get; }
        object IOption.Value => this.Match(value => (object)value, () => null);

        readonly T _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Option(Option.Tags tag, in T value)
        {
            Tag = tag;
            _value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryValue(out T value)
        {
            value = _value;
            return Tag == Option.Tags.Some;
        }

        public Option<TTo> Cast<TTo>() => this.Bind(value => value is TTo casted ? Option.From(casted) : Option.None());
        public bool Equals(Option<T> other) => this == other;
        public bool Equals(T other) => this == other;
        public override bool Equals(object obj) =>
            obj is T value ? this == value :
            obj is Option<T> option ? this == option :
            obj is null || obj is None == this.IsNone();

        public override int GetHashCode() => Tag == Option.Tags.Some ? _value.GetHashCode() : Option.None().GetHashCode();
        public override string ToString() => Tag == Option.Tags.Some ? $"{nameof(Option.Tags.Some)}({_value})" : Option.None().ToString();
    }

    public static class Option
    {
        public enum Tags : byte { None, Some }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Some<T>(in T value) where T : struct => value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<Unit> Some() => Some(default(Unit));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static None None() => new None();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> From<T>(in T value) => value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> From<T>(in T? value) where T : struct => value.HasValue ? From(value.Value) : None();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(this T option, Tags tag) where T : IOption => option.Tag == tag;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(in this Option<T> option, Tags tag) => option.Tag == tag;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSome<T>(this T option) where T : IOption => option.Is(Tags.Some);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSome<T>(in this Option<T> option) => option.Is(Tags.Some);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNone<T>(this T option) where T : IOption => option.Is(Tags.None);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNone<T>(in this Option<T> option) => option.Is(Tags.None);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> AsOption<T>(in this T? value) where T : struct => From(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> AsOption<T>(this None none) => none;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? AsNullable<T>(in this Option<T> option) where T : struct => option.TryValue(out var value) ? (T?)value : null;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<T, None> AsOr<T>(in this Option<T> option) => option.Match(value => Core.Or.Left(value).AsOr<None>(), () => None());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> AsOption<T>(in this Or<T, Unit> or) => or.MapRight(_ => None()).AsOption();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> AsOption<T>(in this Or<T, None> or) => or.Match(value => From(value), none => none);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Try<T>(Func<T> @try, Action @finally = null)
        {
            try { return @try(); }
            catch { return None(); }
            finally { @finally?.Invoke(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Try<TState, T>(in TState state, Func<TState, T> @try, Action<TState> @finally = null)
        {
            try { return @try(state); }
            catch { return None(); }
            finally { @finally?.Invoke(state); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<Unit> Try(Action @try, Action @finally = null)
        {
            try { @try(); return default(Unit); }
            catch { return None(); }
            finally { @finally?.Invoke(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<Unit> Try<TState>(in TState state, Action<TState> @try, Action<TState> @finally = null)
        {
            try { @try(state); return default(Unit); }
            catch { return None(); }
            finally { @finally?.Invoke(state); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Do<T>(in this Option<T> option, Action<T> @do)
        {
            if (option.TryValue(out var value)) @do(value);
            return option;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Do<T, TState>(in this Option<T> option, in TState state, Action<T, TState> @do)
        {
            if (option.TryValue(out var value)) @do(value, state);
            return option;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Or<T, TState>(in this Option<T> option, in TState state, Func<TState, T> provide) =>
            option.TryValue(out var current) ? current : provide(state);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Or<T>(in this Option<T> option, Func<T> provide) =>
            option.TryValue(out var current) ? current : provide();

        public static T Or<T>(in this Option<T> option, in T @default) => option.TryValue(out var value) ? value : @default;
        public static Option<T> Or<T>(in this Option<T> option1, in Option<T> option2) => option1.TryValue(out var value1) ? value1 : option2;
        public static Option<T> Or<T>(in this Option<T> option1, in Option<T> option2, in Option<T> option3) => option1.Or(option2).Or(option3);
        public static Option<T> Or<T>(in this Option<T> option1, in Option<T> option2, in Option<T> option3, in Option<T> option4) => option1.Or(option2).Or(option3).Or(option4);
        public static Option<T> Or<T>(in this Option<T> option1, in Option<T> option2, in Option<T> option3, in Option<T> option4, in Option<T> option5) => option1.Or(option2).Or(option3).Or(option4).Or(option5);

        public static T OrThrow<T>(in this Option<T> option, string message) => option.Or(message, state => throw new InvalidOperationException(state));
        public static T OrThrow<T>(in this Option<T> option) => option.Or(() => throw new InvalidOperationException());
        public static T OrDefault<T>(in this Option<T> option) => option.Or(default(T));
        public static Option<Unit> Ignore<T>(in this Option<T> option) => option.Map(_ => default(Unit));
        public static Option<object> Box<T>(in this Option<T> option) => option.Map(value => (object)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<TOut> Map<TIn, TOut>(in this Option<TIn> option, Func<TIn, TOut> map)
        {
            if (option.TryValue(out var value)) return map(value);
            return None();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<TOut> Map<TIn, TOut, TState>(in this Option<TIn> option, in TState state, Func<TIn, TState, TOut> map)
        {
            if (option.TryValue(out var value)) return map(value, state);
            return None();
        }

        public static Option<T> Filter<T>(in this Option<T> option, bool filter) => filter ? option : None();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Filter<T>(in this Option<T> option, Func<T, bool> filter)
        {
            if (option.TryValue(out var value)) return filter(value) ? option : None();
            return None();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Filter<T, TState>(in this Option<T> option, in TState state, Func<T, TState, bool> filter)
        {
            if (option.TryValue(out var value)) return filter(value, state) ? option : None();
            return None();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Fold<TIn, TOut>(in this Option<TIn> option, in TOut seed, Func<TOut, TIn, TOut> fold)
        {
            if (option.TryValue(out var value)) return fold(seed, value);
            return seed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Fold<TIn, TOut, TState>(in this Option<TIn> option, in TOut seed, in TState state, Func<TOut, TIn, TState, TOut> fold)
        {
            if (option.TryValue(out var value)) return fold(seed, value, state);
            return seed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Match<TIn, TOut>(in this Option<TIn> option, Func<TIn, TOut> some, Func<TOut> none) =>
            option.TryValue(out var value) ? some(value) : none();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Match<TIn, TOut, TState>(in this Option<TIn> option, in TState state, Func<TIn, TState, TOut> some, Func<TState, TOut> none) =>
            option.TryValue(out var value) ? some(value, state) : none(state);

        public static Option<(T1, T2)> And<T1, T2>(in this Option<T1> left, in T2 right)
        {
            if (left.TryValue(out var value1)) return (value1, right);
            return None();
        }

        public static Option<(T1, T2)> And<T1, T2>(in this Option<T1> left, in Option<T2> right)
        {
            if (left.TryValue(out var value1) && right.TryValue(out var value2)) return (value1, value2);
            return None();
        }

        public static Option<(T1, T2, T3)> And<T1, T2, T3>(in this Option<(T1, T2)> left, in T3 right)
        {
            if (left.TryValue(out var value1)) return (value1.Item1, value1.Item2, right);
            return None();
        }

        public static Option<(T1, T2, T3)> And<T1, T2, T3>(in this Option<(T1, T2)> left, in Option<T3> right)
        {
            if (left.TryValue(out var value1) && right.TryValue(out var value2)) return (value1.Item1, value1.Item2, value2);
            return None();
        }

        public static Option<(T1, T2, T3, T4)> And<T1, T2, T3, T4>(in this Option<(T1, T2, T3)> left, in T4 right)
        {
            if (left.TryValue(out var value1)) return (value1.Item1, value1.Item2, value1.Item3, right);
            return None();
        }

        public static Option<(T1, T2, T3, T4)> And<T1, T2, T3, T4>(in this Option<(T1, T2, T3)> left, in Option<T4> right)
        {
            if (left.TryValue(out var value1) && right.TryValue(out var value2)) return (value1.Item1, value1.Item2, value1.Item3, value2);
            return None();
        }

        public static Option<(T1, T2, T3, T4, T5)> And<T1, T2, T3, T4, T5>(in this Option<(T1, T2, T3, T4)> left, in T5 right)
        {
            if (left.TryValue(out var value1)) return (value1.Item1, value1.Item2, value1.Item3, value1.Item4, right);
            return None();
        }

        public static Option<(T1, T2, T3, T4, T5)> And<T1, T2, T3, T4, T5>(in this Option<(T1, T2, T3, T4)> left, in Option<T5> right)
        {
            if (left.TryValue(out var value1) && right.TryValue(out var value2)) return (value1.Item1, value1.Item2, value1.Item3, value1.Item4, value2);
            return None();
        }

        public static Option<(T1, T2, T3)> And<T1, T2, T3>(in this Option<T1> option1, in Option<T2> option2, in Option<T3> option3)
        {
            if (option1.TryValue(out var value1) && option2.TryValue(out var value2) && option3.TryValue(out var value3)) return (value1, value2, value3);
            return None();
        }

        public static Option<(T1, T2, T3, T4)> And<T1, T2, T3, T4>(in this Option<T1> option1, in Option<T2> option2, in Option<T3> option3, in Option<T4> option4)
        {
            if (option1.TryValue(out var value1) && option2.TryValue(out var value2) && option3.TryValue(out var value3) && option4.TryValue(out var value4)) return (value1, value2, value3, value4);
            return None();
        }

        public static Option<(T1, T2, T3, T4, T5)> And<T1, T2, T3, T4, T5>(in this Option<T1> option1, in Option<T2> option2, in Option<T3> option3, in Option<T4> option4, in Option<T5> option5)
        {
            if (option1.TryValue(out var value1) && option2.TryValue(out var value2) && option3.TryValue(out var value3) && option4.TryValue(out var value4) && option5.TryValue(out var value5)) return (value1, value2, value3, value4, value5);
            return None();
        }

        public static Option<T1> Left<T1, T2>(in this Option<(T1, T2)> option) => option.Map(pair => pair.Item1);
        public static Option<T2> Right<T1, T2>(in this Option<(T1, T2)> option) => option.Map(pair => pair.Item2);

        public static Option<TOut> Return<TIn, TOut>(in this Option<TIn> option, TOut value)
        {
            if (option.IsSome()) return value;
            return None();
        }

        public static Option<T> Flatten<T>(in this Option<Option<T>> option)
        {
            if (option.TryValue(out var value)) return value;
            return None();
        }

        public static IOption Flatten<T>(in this Option<T> option) where T : IOption
        {
            if (option.TryValue(out var value)) return value;
            return None();
        }

        public static Option<T> Flatten<T>(in this Option<T>? option)
        {
            if (option.HasValue) return option.Value;
            return None();
        }

        public static Option<T> Flatten<T>(in this Option<T?> option) where T : struct
        {
            if (option.TryValue(out var value)) return value.AsOption();
            return None();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<TOut> Bind<TIn, TOut>(in this Option<TIn> option, Func<TIn, Option<TOut>> bind)
        {
            if (option.TryValue(out var value)) return bind(value);
            return None();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<TOut> Bind<TIn, TOut, TState>(in this Option<TIn> option, in TState state, Func<TIn, TState, Option<TOut>> bind)
        {
            if (option.TryValue(out var value)) return bind(value, state);
            return None();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Recover<T>(in this Option<T> option, Func<Option<T>> recover) => option.IsNone() ? recover() : option;

        public static bool TryTake<T>(ref this Option<T> option, out T value) => option.Take().TryValue(out value);

        public static Option<T> Take<T>(ref this Option<T> option)
        {
            var copy = option;
            option = None();
            return copy;
        }

        public static Option<T[]> All<T>(this Option<T>[] options)
        {
            if (options.Length == 0) return From(Array.Empty<T>());

            var values = new T[options.Length];
            for (var i = 0; i < options.Length; i++)
            {
                if (options[i].TryValue(out values[i])) continue;
                else return None();
            }
            return values;
        }

        public static Option<T[]> All<T>(this IEnumerable<Option<T>> options) => All(options.ToArray());

        public static Option<T> Any<T>(this Option<T>[] options)
        {
            foreach (var option in options) if (option.TryValue(out var value)) return value;
            return None();
        }

        public static Option<T> Any<T>(this IEnumerable<Option<T>> options)
        {
            foreach (var option in options) if (option.TryValue(out var value)) return value;
            return None();
        }

        public static IEnumerable<T> Choose<T>(this Option<T>[] options)
        {
            foreach (var option in options) if (option.TryValue(out var value)) yield return value;
        }

        public static IEnumerable<T> Choose<T>(this IEnumerable<Option<T>> options)
        {
            foreach (var option in options) if (option.TryValue(out var value)) yield return value;
        }

        public static IEnumerable<TResult> Choose<TSource, TResult>(this TSource[] source, Func<TSource, Option<TResult>> map)
        {
            foreach (var item in source) if (map(item).TryValue(out var value)) yield return value;
        }

        public static IEnumerable<TResult> Choose<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Option<TResult>> map)
        {
            foreach (var item in source) if (map(item).TryValue(out var value)) yield return value;
        }

        public static Option<T> FirstOrNone<T>(this IEnumerable<T> source)
        {
            foreach (var item in source) return item;
            return None();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (var item in source) if (predicate(item)) return item;
            return None();
        }

        public static Option<T> FirstOrNone<T>(this T[] source)
        {
            if (source.Length > 0) return source[0];
            return None();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> FirstOrNone<T>(this T[] source, Func<T, bool> predicate)
        {
            foreach (var item in source) if (predicate(item)) return item;
            return None();
        }

        public static Option<T> LastOrNone<T>(this IEnumerable<T> source)
        {
            var option = None().AsOption<T>();
            foreach (var item in source) option = item;
            return option;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> LastOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var option = None().AsOption<T>();
            foreach (var item in source) if (predicate(item)) option = item;
            return option;
        }

        public static Option<T> LastOrNone<T>(this T[] source)
        {
            if (source.Length > 0) return source[source.Length - 1];
            return None();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> LastOrNone<T>(this T[] source, Func<T, bool> predicate)
        {
            for (int i = source.Length - 1; i >= 0; i--)
            {
                var item = source[i];
                if (predicate(item)) return item;
            }
            return None();
        }

        public static Option<T> Cast<T>(object value) => From(value).Cast<T>();
        public static Option<TOut> Cast<TIn, TOut>(in TIn value) => From(value).Cast<TOut>();
    }
}
