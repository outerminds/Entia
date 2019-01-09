using System;
using System.Collections.Generic;

namespace Entia.Core
{
    public interface IOption
    {
        Option.Tags Tag { get; }
        Option<T> Cast<T>();
    }

    public readonly struct Some<T> : IOption
    {
        public static readonly Some<T> Empty = new Some<T>(default);

        public readonly T Value;
        public Some(in T value) { Value = value; }

        Option.Tags IOption.Tag => Option.Tags.Some;
        Option<TTo> IOption.Cast<TTo>() => this.AsOption().Cast<TTo>();

        public static implicit operator Some<T>(in T value) => new Some<T>(value);
        public static implicit operator Some<Unit>(in Some<T> _) => default(Unit);
        public static implicit operator Option<Unit>(in Some<T> _) => default(Unit);
    }

    public readonly struct None : IOption
    {
        public static readonly None Empty = new None();

        Option.Tags IOption.Tag => Option.Tags.None;
        Option<T> IOption.Cast<T>() => Option.None();
    }

    public readonly struct Option<T> : IOption
    {
        public Option.Tags Tag { get; }

        readonly T _value;

        public Option<TTo> Cast<TTo>() => this.Bind(value => value is TTo casted ? Option.Some(casted).AsOption() : Option.None());

        Option(Option.Tags tag, in T value)
        {
            Tag = tag;
            _value = value;
        }

        public static implicit operator Option<T>(in T value) => Option.Some(value);

        public static implicit operator Option<T>(in Some<T> some) => new Option<T>(Option.Tags.Some, some.Value);
        public static explicit operator Some<T>(in Option<T> option) => option.Tag == Option.Tags.Some ?
            Option.Some(option._value) : throw new InvalidCastException();

        public static implicit operator Option<T>(in None none) => default;
        public static explicit operator None(in Option<T> option) => option.Tag == Option.Tags.None ?
            Option.None() : throw new InvalidCastException();

        public static implicit operator Option<Unit>(in Option<T> option) => option.Map(_ => default(Unit));
    }

    public static class Option
    {
        public enum Tags : byte { None, Some }

        public static Some<T> Some<T>(in T value) => new Some<T>(value);
        public static None None() => new None();

        public static Option<T> Try<T>(Func<T> @try)
        {
            try { return @try(); }
            catch { return None(); }
        }

        public static Option<Unit> Try(Action @try)
        {
            try { @try(); return default(Unit); }
            catch { return None(); }
        }

        public static bool Is<T>(in this Option<T> option, Tags tag) => option.Tag == tag;
        public static bool IsSome<T>(in this Option<T> option) => option.Is(Tags.Some);
        public static bool IsNone<T>(in this Option<T> option) => option.Is(Tags.None);
        public static Option<T> AsOption<T>(in this Some<T> some) => some;
        public static Option<T> AsOption<T>(in this None none) => none;

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

        public static bool TryNone<T>(in this Option<T> option, out None none)
        {
            if (option.IsNone())
            {
                none = (None)option;
                return true;
            }

            none = default;
            return false;
        }

        public static Some<TOut> Map<TIn, TOut>(in this Some<TIn> some, Func<TIn, TOut> map) => map(some.Value);
        public static Some<T> Flatten<T>(in this Some<Some<T>> some) => some.Value;
        public static None Flatten(in this Some<None> some) => some.Value;
        public static Option<T> Flatten<T>(in this Some<Option<T>> some) => some.Value;
        public static Option<TOut> Bind<TIn, TOut>(in this Some<TIn> some, Func<TIn, Option<TOut>> bind) => bind(some.Value);
        public static Some<TOut> Bind<TIn, TOut>(in this Some<TIn> some, Func<TIn, Some<TOut>> bind) => bind(some.Value);
        public static None Bind<T>(in this Some<T> some, Func<T, None> bind) => bind(some.Value);

        public static Option<Unit> Do<T>(in this Option<T> option, Action<T> @do)
        {
            if (option.TryValue(out var value)) { @do(value); return default(Unit); }
            else if (option.TryNone(out var none)) return none;
            else return None();
        }

        public static Option<Unit> Do<T, TState>(in this Option<T> option, Action<T, TState> @do, TState state)
        {
            if (option.TryValue(out var value)) { @do(value, state); return default(Unit); }
            else if (option.TryNone(out var none)) return none;
            else return None();
        }

        public static T Or<T>(in this Option<T> option, in T value) => option.TryValue(out var current) ? current : value;

        public static T Or<T>(in this Option<T> option, Func<T> provide) => option.TryValue(out var current) ? current : provide();

        public static Option<object> Box<T>(this T option) where T : IOption => option.Cast<object>();

        public static Option<TOut> Map<TIn, TOut>(in this Option<TIn> option, Func<TIn, TOut> map)
        {
            if (option.TryValue(out var value)) return map(value);
            return None();
        }

        public static Option<TOut> Map<TIn, TOut, TState>(in this Option<TIn> option, Func<TIn, TState, TOut> map, in TState state)
        {
            if (option.TryValue(out var value)) return map(value, state);
            return None();
        }

        public static TOut Match<TIn, TOut>(in this Option<TIn> option, Func<TIn, TOut> some, Func<TOut> none)
        {
            if (option.TryValue(out var value)) return some(value);
            return none();
        }

        public static TOut Match<TIn, TOut>(in this Option<TIn> option, Func<TIn, TOut> some, TOut none)
        {
            if (option.TryValue(out var value)) return some(value);
            return none;
        }

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

        public static Option<TOut> Bind<TIn, TOut>(in this Option<TIn> option, Func<TIn, Option<TOut>> bind)
        {
            if (option.TryValue(out var value)) return bind(value);
            return None();
        }

        public static Option<TOut> Bind<TIn, TOut, TState>(in this Option<TIn> option, Func<TIn, TState, Option<TOut>> bind, in TState state)
        {
            if (option.TryValue(out var value)) return bind(value, state);
            return None();
        }

        public static Option<T> Recover<T>(in this Option<T> option, Func<Option<T>> recover) =>
            option.IsNone() ? recover() : option;

        public static Option<T[]> All<T>(this IEnumerable<Option<T>> options)
        {
            var values = new List<T>();

            foreach (var option in options)
            {
                if (option.TryValue(out var value)) values.Add(value);
                else return None();
            }

            return values.ToArray();
        }

        public static Option<T> Any<T>(this IEnumerable<Option<T>> options)
        {
            foreach (var option in options)
                if (option.TrySome(out var some)) return some;

            return None();
        }

        public static IEnumerable<T> Choose<T>(this IEnumerable<Option<T>> options)
        {
            foreach (var option in options) if (option.TryValue(out var value)) yield return value;
        }

        public static Option<T> Cast<T>(object value) => Some(value).AsOption().Cast<T>();

        public static Option<TOut> Cast<TIn, TOut>(in TIn value) => Some(value).AsOption().Cast<TOut>();
    }
}
