using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Entia.Core
{
    public interface IOption
    {
        Option.Tags Tag { get; }
        Option<T> Cast<T>();
    }

    public readonly struct Some<T> : IOption, IEquatable<Some<T>>
    {
        public static readonly Some<T> Empty = new Some<T>(default);

        public static bool operator ==(in Some<T> left, in T right) => EqualityComparer<T>.Default.Equals(left.Value, right);
        public static bool operator !=(in Some<T> left, in T right) => !(left == right);
        public static bool operator ==(in T left, in Some<T> right) => right == left;
        public static bool operator !=(in T left, in Some<T> right) => !(left == right);
        public static bool operator ==(in Some<T> left, in Some<T> right) => right == left.Value;
        public static bool operator !=(in Some<T> left, in Some<T> right) => !(left == right);

        public static implicit operator Some<T>(in T value) => new Some<T>(value);
        public static implicit operator Some<Unit>(in Some<T> _) => new Some<Unit>(default);
        public static implicit operator Option<Unit>(in Some<T> _) => default(Unit);

        Option.Tags IOption.Tag => Option.Tags.Some;

        public readonly T Value;
        public Some(in T value) { Value = value; }

        Option<T1> IOption.Cast<T1>() => Option.Cast<T1>(Value);

        public bool Equals(Some<T> other) => EqualityComparer<T>.Default.Equals(Value, other.Value);
        public override bool Equals(object obj) =>
            obj is T value ? this == value :
            obj is Some<T> some ? this == some :
            false;

        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Value);
        public override string ToString() => $"{GetType().Format()}({Value})";
    }

    public readonly struct None : IOption, IEquatable<None>
    {
        public static bool operator ==(None left, None right) => true;
        public static bool operator !=(None left, None right) => !(left == right);

        Option.Tags IOption.Tag => Option.Tags.None;
        Option<T> IOption.Cast<T>() => Option.None();

        public bool Equals(None other) => this == other;
        public override bool Equals(object obj) => obj is null || obj is None;
        public override int GetHashCode() => 0;
        public override string ToString() => GetType().Format();
    }

    public readonly struct Option<T> : IOption, IEquatable<Option<T>>, IEquatable<Some<T>>, IEquatable<T>
    {
        static readonly Func<T, bool> _isNull = typeof(T).IsValueType ?
            new Func<T, bool>(_ => false) : new Func<T, bool>(value => value == null);

        public static implicit operator Option<T>(in Some<T> some) => new Option<T>(Option.Tags.Some, some.Value);
        public static implicit operator Option<T>(in T value) => _isNull(value) ?
            new Option<T>(Option.Tags.None, value) :
            new Option<T>(Option.Tags.Some, value);

        public static implicit operator Option<T>(in None none) => new Option<T>(Option.Tags.None, default);
        public static implicit operator Option<Unit>(in Option<T> option) => new Option<Unit>(option.Tag, default);
        public static implicit operator bool(in Option<T> option) => option.Tag == Option.Tags.Some;
        public static explicit operator Some<T>(in Option<T> option) => option.Tag == Option.Tags.Some ? new Some<T>(option._value) : throw new InvalidCastException();
        public static explicit operator None(in Option<T> option) => option.Tag == Option.Tags.None ? new None() : throw new InvalidCastException();
        public static bool operator ==(in Option<T> left, in T right) => left.TryValue(out var value) && EqualityComparer<T>.Default.Equals(value, right);
        public static bool operator !=(in Option<T> left, in T right) => !(left == right);
        public static bool operator ==(in T left, in Option<T> right) => right == left;
        public static bool operator !=(in T left, in Option<T> right) => !(left == right);
        public static bool operator ==(in Option<T> left, in Option<T> right) => left.TryValue(out var value) ? right == value : right.IsNone();
        public static bool operator !=(in Option<T> left, in Option<T> right) => !(left == right);
        public static bool operator ==(in Option<T> left, in Some<T> right) => left == right.Value;
        public static bool operator !=(in Option<T> left, in Some<T> right) => !(left == right);
        public static bool operator ==(in Some<T> left, in Option<T> right) => right == left.Value;
        public static bool operator !=(in Some<T> left, in Option<T> right) => !(left == right);
        public static bool operator ==(in Option<T> left, None right) => left.IsNone();
        public static bool operator !=(in Option<T> left, None right) => !(left == right);
        public static bool operator ==(None left, in Option<T> right) => right == left;
        public static bool operator !=(None left, in Option<T> right) => !(left == right);

        public Option.Tags Tag { get; }

        readonly T _value;

        public Option<TTo> Cast<TTo>() => this.Bind(value => value is TTo casted ? Option.From(casted) : Option.None());

        Option(Option.Tags tag, in T value)
        {
            Tag = tag;
            _value = value;
        }

        public bool Equals(Option<T> other) => this == other;
        public bool Equals(T other) => this == other;
        public bool Equals(Some<T> other) => this == other;
        public override bool Equals(object obj) =>
            obj is T value ? this == value :
            obj is null ? this.IsNone() :
            obj is Option<T> option ? this == option :
            obj is Some<T> some ? this == some :
            obj is None none ? this == none :
            false;

        public override int GetHashCode() => this.TryValue(out var value) ? EqualityComparer<T>.Default.GetHashCode(value) : 0;

        public override string ToString()
        {
            return
                this.TrySome(out var some) ? some.ToString() :
                this.IsNone() ? Option.None().ToString() :
                base.ToString();
        }
    }

    public static class Option
    {
        public enum Tags : byte { None, Some }

        public static Some<T> Some<T>(in T value) where T : struct => new Some<T>(value);
        public static Some<Unit> Some() => new Some<Unit>(default);
        public static None None() => new None();
        public static Option<T> From<T>(in T value) => value;
        public static Option<T> From<T>(in T? value) where T : struct => value.HasValue ? From(value.Value) : None();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Try<T>(Func<T> @try)
        {
            try { return @try(); }
            catch { return None(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Try<TState, T>(in TState state, Func<TState, T> @try)
        {
            try { return @try(state); }
            catch { return None(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<Unit> Try(Action @try)
        {
            try { @try(); return default(Unit); }
            catch { return None(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<Unit> Try<TState>(in TState input, Action<TState> @try)
        {
            try { @try(input); return default(Unit); }
            catch { return None(); }
        }

        public static bool Is<T>(in this Option<T> option, Tags tag) => option.Tag == tag;
        public static bool IsSome<T>(in this Option<T> option) => option.Is(Tags.Some);
        public static bool IsNone<T>(in this Option<T> option) => option.Is(Tags.None);
        public static Option<T> AsOption<T>(in this T? value) where T : struct => From(value);
        public static Option<T> AsOption<T>(in this Some<T> some) => some;
        public static Option<T> AsOption<T>(in this None none) => none;
        public static Option<Unit> AsOption(in this None none) => none;

        public static bool TrySome<T>(in this Option<T> option, out Some<T> some)
        {
            if (option.IsSome())
            {
                some = (Some<T>)option;
                return true;
            }

            some = default;
            return false;
        }

        public static bool TryValue<T>(in this Option<T> option, out T value)
        {
            if (option.TrySome(out var some))
            {
                value = some.Value;
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Some<TOut> Map<TIn, TOut>(in this Some<TIn> some, Func<TIn, TOut> map) => map(some.Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Some<TOut> Map<TIn, TOut, TState>(in this Some<TIn> some, in TState state, Func<TIn, TState, TOut> map) => map(some.Value, state);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Filter<T>(in this Some<T> some, Func<T, bool> filter) => filter(some.Value) ? some.AsOption() : None();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Filter<T, TState>(in this Some<T> some, in TState state, Func<T, TState, bool> filter) => filter(some.Value, state) ? some.AsOption() : None();

        public static Some<T> Flatten<T>(in this Some<Some<T>> some) => some.Value;
        public static None Flatten(in this Some<None> some) => some.Value;
        public static Option<T> Flatten<T>(in this Some<Option<T>> some) => some.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<TOut> Bind<TIn, TOut>(in this Some<TIn> some, Func<TIn, Option<TOut>> bind) => bind(some.Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Some<TOut> Bind<TIn, TOut>(in this Some<TIn> some, Func<TIn, Some<TOut>> bind) => bind(some.Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static None Bind<T>(in this Some<T> some, Func<T, None> bind) => bind(some.Value);

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
        public static T Or<T>(in this Option<T> option, Func<T> provide) => option.TryValue(out var current) ? current : provide();

        public static T Or<T>(in this Option<T> option, in T value) => option.TryValue(out var current) ? current : value;
        public static T OrDefault<T>(in this Option<T> option) => option.Or(default(T));
        public static Some<Unit> Ignore<T>(in this Some<T> some) => some;
        public static Option<Unit> Ignore<T>(in this Option<T> option) => option;
        public static Option<object> Box<T>(this T option) where T : IOption => option.Cast<object>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<TOut> Map<TIn, TOut>(in this Option<TIn> option, Func<TIn, TOut> map)
        {
            if (option.TryValue(out var value)) return map(value);
            else if (option.IsNone()) return None();
            else return None();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<TOut> Map<TIn, TOut, TState>(in this Option<TIn> option, in TState state, Func<TIn, TState, TOut> map)
        {
            if (option.TryValue(out var value)) return map(value, state);
            else if (option.IsNone()) return None();
            else return None();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Filter<T>(in this Option<T> option, Func<T, bool> filter)
        {
            if (option.TryValue(out var value)) return filter(value) ? option : None();
            else return None();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Filter<T, TState>(in this Option<T> option, in TState state, Func<T, TState, bool> filter)
        {
            if (option.TryValue(out var value)) return filter(value, state) ? option : None();
            else return None();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Fold<TIn, TOut>(in this Option<TIn> option, in TOut seed, Func<TOut, TIn, TOut> fold)
        {
            if (option.TryValue(out var value)) return fold(seed, value);
            else return seed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Fold<TIn, TOut, TState>(in this Option<TIn> option, in TOut seed, in TState state, Func<TOut, TIn, TState, TOut> fold)
        {
            if (option.TryValue(out var value)) return fold(seed, value, state);
            else return seed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Match<TIn, TOut>(in this Option<TIn> option, Func<TIn, TOut> some, Func<TOut> none) =>
            option.TryValue(out var value) ? some(value) : none();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Match<T>(in this Option<T> option, Action<T> some, Action none)
        {
            if (option.TryValue(out var value)) some(value);
            else none();
            return option;
        }

        public static Option<(T1 value1, T2 value2)> And<T1, T2>(in this Option<T1> left, in Option<T2> right) =>
            All(left, right);
        public static Option<(T1 value1, T2 value2, T3 value3)> And<T1, T2, T3>(in this Option<(T1 value1, T2 value2)> left, in Option<T3> right) =>
            All(left, right).Map(pair => (pair.value1.value1, pair.value1.value2, pair.value2));
        public static Option<(T1 value1, T2 value2, T3 value3, T4 value4)> And<T1, T2, T3, T4>(in this Option<(T1 value1, T2 value2, T3 value3)> left, in Option<T4> right) =>
            All(left, right).Map(pair => (pair.value1.value1, pair.value1.value2, pair.value1.value3, pair.value2));
        public static Option<(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)> And<T1, T2, T3, T4, T5>(in this Option<(T1 value1, T2 value2, T3 value3, T4 value4)> left, in Option<T5> right) =>
            All(left, right).Map(pair => (pair.value1.value1, pair.value1.value2, pair.value1.value3, pair.value1.value4, pair.value2));
        public static Option<T1> Left<T1, T2>(in this Option<T1> left, in Option<T2> right) =>
            All(left, right).Map(pair => pair.value1);
        public static Option<T2> Right<T1, T2>(in this Option<T1> left, in Option<T2> right) =>
            All(left, right).Map(pair => pair.value2);

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

        public static Option<(T1 value1, T2 value2)> All<T1, T2>(in Option<T1> option1, in Option<T2> option2)
        {
            if (option1.TryValue(out var value1) && option2.TryValue(out var value2)) return (value1, value2);
            return None();
        }

        public static Option<(T1 value1, T2 value2, T3 value3)> All<T1, T2, T3>(in Option<T1> option1, in Option<T2> option2, in Option<T3> option3)
        {
            if (option1.TryValue(out var value1) && option2.TryValue(out var value2) && option3.TryValue(out var value3)) return (value1, value2, value3);
            return None();
        }

        public static Option<(T1 value1, T2 value2, T3 value3, T4 value4)> All<T1, T2, T3, T4>(in Option<T1> option1, in Option<T2> option2, in Option<T3> option3, in Option<T4> option4)
        {
            if (option1.TryValue(out var value1) && option2.TryValue(out var value2) && option3.TryValue(out var value3) && option4.TryValue(out var value4)) return (value1, value2, value3, value4);
            return None();
        }

        public static Option<(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)> All<T1, T2, T3, T4, T5>(in Option<T1> option1, in Option<T2> option2, in Option<T3> option3, in Option<T4> option4, in Option<T5> option5)
        {
            if (option1.TryValue(out var value1) && option2.TryValue(out var value2) && option3.TryValue(out var value3) && option4.TryValue(out var value4) && option5.TryValue(out var value5)) return (value1, value2, value3, value4, value5);
            return None();
        }

        public static Option<T[]> All<T>(params Option<T>[] options)
        {
            var values = new T[options.Length];
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                if (option.TryValue(out var value)) values[i] = value;
                else return None();
            }
            return values;
        }

        public static Option<T[]> All<T>(this IEnumerable<Option<T>> options) => All(options.ToArray());
        public static Option<Unit> All(this IEnumerable<Option<Unit>> options) => options.All<Unit>().Return(default(Unit));
        public static Option<Unit> All(this IEnumerable<None> nones) => nones.Select(none => none.AsOption()).All();
        public static Option<T> Any<T>(this Option<T>[] options) => options.AsEnumerable().Any();

        public static Option<T> Any<T>(this IEnumerable<Option<T>> options)
        {
            foreach (var option in options) if (option.TrySome(out var some)) return some;
            return None();
        }

        public static Option<Unit> Any(this IEnumerable<Option<Unit>> options) => options.Any<Unit>().Return(default(Unit));

        public static IEnumerable<T> Choose<T>(params Option<T>[] options)
        {
            foreach (var option in options) if (option.TryValue(out var value)) yield return value;
        }

        public static IEnumerable<T> Choose<T>(this IEnumerable<Option<T>> options)
        {
            foreach (var option in options) if (option.TryValue(out var value)) yield return value;
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

        public static Option<T> Cast<T>(object value) => From(value).Cast<T>();
        public static Option<TOut> Cast<TIn, TOut>(in TIn value) => From(value).Cast<TOut>();
    }
}