using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Entia.Core
{
    public interface IResult
    {
        Result.Tags Tag { get; }
        Result<T> Cast<T>();
    }

    public readonly struct Success<T> : IResult
    {
        public static readonly Success<T> Empty = new Success<T>(default);

        Result.Tags IResult.Tag => Result.Tags.Success;

        public readonly T Value;
        public Success(in T value) { Value = value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Result<T1> IResult.Cast<T1>() => Result.Cast<T1>(Value);

        public override string ToString() => $"{GetType().Format()}({Value})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Success<T>(in T value) => new Success<T>(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Success<Unit>(in Success<T> _) => new Success<Unit>(default);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Some<T>(in Success<T> success) => success.Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Some<Unit>(in Success<T> _) => default(Unit);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Option<T>(in Success<T> success) => success.Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Option<Unit>(in Success<T> _) => default(Unit);
    }

    public readonly struct Failure : IResult
    {
        Result.Tags IResult.Tag => Result.Tags.Failure;

        public readonly string[] Messages;
        public Failure(string[] messages) { Messages = messages; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Result<T> IResult.Cast<T>() => Result.Failure(Messages);

        public override string ToString() => $"{GetType().Format()}({string.Join(", ", Messages)})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator None(in Failure failure) => Option.None();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Failure(in None none) => Result.Failure();
    }

    public readonly struct Result<T> : IResult
    {
        public Result.Tags Tag { get; }

        readonly T _value;
        readonly string[] _messages;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<TTo> Cast<TTo>() => this.Bind(value =>
            value is TTo casted ? Result.Success(casted).AsResult() :
            Result.Failure($"Expected value '{value?.ToString() ?? "null"}' to be of type '{typeof(TTo)}'."));

        Result(Result.Tags tag, in T value, string[] messages)
        {
            Tag = tag;
            _value = value;
            _messages = messages;
        }

        public override string ToString()
        {
            switch (Tag)
            {
                case Result.Tags.Success: return ((Success<T>)this).ToString();
                case Result.Tags.Failure: return ((Failure)this).ToString();
                default: return base.ToString();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Result<T>(in T value) => new Result<T>(Result.Tags.Success, value, null);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Result<T>(in Success<T> success) => new Result<T>(Result.Tags.Success, success.Value, null);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Option<T>(in Result<T> result) => result.TryValue(out var value) ? Option.Some(value).AsOption() : Option.None();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Result<T>(in Option<T> option) => option.TryValue(out var value) ? Result.Success(value).AsResult() : Result.Failure();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Result<T>(in Failure failure) => new Result<T>(Result.Tags.Failure, default, failure.Messages);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Result<Unit>(in Result<T> result) => new Result<Unit>(result.Tag, default, result._messages);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(in Result<T> result) => result.Tag == Result.Tags.Success;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Success<T>(in Result<T> result) => result.Tag == Result.Tags.Success ?
            Result.Success(result._value) : throw new InvalidCastException();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Failure(in Result<T> result) => result.Tag == Result.Tags.Failure ?
            Result.Failure(result._messages) : throw new InvalidCastException();
    }

    public static class Result
    {
        public enum Tags : byte { None, Success, Failure }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Success<T> Success<T>(in T value) => new Success<T>(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Success<Unit> Success() => new Success<Unit>(default);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure Failure() => new Failure(Array.Empty<string>());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure Failure(params string[] messages) => new Failure(messages);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure Failure(IEnumerable<string> messages) => Failure(messages.ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure Exception(Exception exception) => Failure(exception.ToString());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Try<T>(Func<T> @try)
        {
            try { return @try(); }
            catch (Exception exception) { return Exception(exception); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Try<TState, T>(in TState state, Func<TState, T> @try)
        {
            try { return @try(state); }
            catch (Exception exception) { return Failure(exception.ToString()); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit> Try(Action @try)
        {
            try { @try(); return default(Unit); }
            catch (Exception exception) { return Failure(exception.ToString()); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit> Try<TState>(in TState input, Action<TState> @try)
        {
            try { @try(input); return default(Unit); }
            catch (Exception exception) { return Failure(exception.ToString()); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(in this Result<T> result, Tags tag) => result.Tag == tag;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSuccess<T>(in this Result<T> result) => result.Is(Tags.Success);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFailure<T>(in this Result<T> result) => result.Is(Tags.Failure);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> AsResult<T>(in this Success<T> success) => success;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> AsResult<T>(in this Failure failure) => failure;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit> AsResult(in this Failure failure) => failure;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Success<T> AsResult<T>(in this Some<T> some) => some.Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure AsResult(in this None none, params string[] messages) => new Failure(messages);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> AsResult<T>(in this Option<T> option, params string[] messages) =>
            option.TryValue(out var value) ? Success(value).AsResult() : Failure(messages);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> AsOption<T>(in this Result<T> result) => result;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure AsFailure<T>(in this Result<T> result) => result.TryFailure(out var failure) ? failure : Failure();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TrySuccess<T>(in this Result<T> result, out Success<T> success)
        {
            if (result.IsSuccess())
            {
                success = (Success<T>)result;
                return true;
            }

            success = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryValue<T>(in this Result<T> result, out T value)
        {
            if (result.TrySuccess(out var success))
            {
                value = success.Value;
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFailure<T>(in this Result<T> result, out Failure failure)
        {
            if (result.IsFailure())
            {
                failure = (Failure)result;
                return true;
            }

            failure = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryMessages<T>(in this Result<T> result, out string[] messages)
        {
            if (result.TryFailure(out var failure))
            {
                messages = failure.Messages;
                return true;
            }

            messages = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string[] Messages<T>(in this Result<T> result) => result.TryMessages(out var messages) ? messages : Array.Empty<string>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Success<TOut> Map<TIn, TOut>(in this Success<TIn> success, Func<TIn, TOut> map) => map(success.Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Filter<T>(in this Success<T> success, Func<T, bool> filter) => filter(success.Value) ? success.AsResult() : Failure();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Filter<T, TState>(in this Success<T> success, in TState state, Func<T, TState, bool> filter) => filter(success.Value, state) ? success.AsResult() : Failure();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Success<T> Flatten<T>(in this Success<Success<T>> success) => success.Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure Flatten(in this Success<Failure> success) => success.Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Flatten<T>(in this Success<Result<T>> success) => success.Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Bind<TIn, TOut>(in this Success<TIn> success, Func<TIn, Result<TOut>> bind) => bind(success.Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Success<TOut> Bind<TIn, TOut>(in this Success<TIn> success, Func<TIn, Success<TOut>> bind) => bind(success.Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure Bind<T>(in this Success<T> success, Func<T, Failure> bind) => bind(success.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Do<T>(in this Result<T> result, Action<T> @do)
        {
            if (result.TryValue(out var value)) @do(value);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Do<T, TState>(in this Result<T> result, in TState state, Action<T, TState> @do)
        {
            if (result.TryValue(out var value)) @do(value, state);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Or<T, TState>(in this Result<T> result, in TState state, Func<TState, T> provide) =>
            result.TryValue(out var current) ? current : provide(state);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Or<T>(in this Result<T> result, Func<T> provide) => result.TryValue(out var current) ? current : provide();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Or<T>(in this Result<T> result, in T value) => result.TryValue(out var current) ? current : value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T OrDefault<T>(in this Result<T> result) => result.Or(default(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Success<Unit> Ignore<T>(in this Success<T> success) => success;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit> Ignore<T>(in this Result<T> result) => result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<object> Box<T>(this T result) where T : IResult => result.Cast<object>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Map<TIn, TOut>(in this Result<TIn> result, Func<TIn, TOut> map)
        {
            if (result.TryValue(out var value)) return map(value);
            else if (result.TryFailure(out var failure)) return failure;
            else return Failure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Map<TIn, TOut, TState>(in this Result<TIn> result, in TState state, Func<TIn, TState, TOut> map)
        {
            if (result.TryValue(out var value)) return map(value, state);
            else if (result.TryFailure(out var failure)) return failure;
            else return Failure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Filter<T>(in this Result<T> result, Func<T, bool> filter)
        {
            if (result.TryValue(out var value)) return filter(value) ? result : Failure();
            else return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Filter<T, TState>(in this Result<T> result, in TState state, Func<T, TState, bool> filter)
        {
            if (result.TryValue(out var value)) return filter(value, state) ? result : Failure();
            else return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Fold<TIn, TOut>(in this Result<TIn> result, in TOut seed, Func<TOut, TIn, TOut> fold)
        {
            if (result.TryValue(out var value)) return fold(seed, value);
            else return seed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Fold<TIn, TOut, TState>(in this Result<TIn> result, in TOut seed, in TState state, Func<TOut, TIn, TState, TOut> fold)
        {
            if (result.TryValue(out var value)) return fold(seed, value, state);
            else return seed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Match<TIn, TOut>(in this Result<TIn> result, Func<TIn, TOut> success, Func<string[], TOut> failure)
        {
            if (result.TryValue(out var value)) return success(value);
            else if (result.TryMessages(out var messages)) return failure(messages);
            else return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Match<T>(in this Result<T> result, Action<T> success, Action<string[]> failure)
        {
            if (result.TryValue(out var value)) success(value);
            else if (result.TryMessages(out var messages)) failure(messages);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<(T1 value1, T2 value2)> And<T1, T2>(in this Result<T1> left, in Result<T2> right) =>
            All(left, right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<(T1 value1, T2 value2, T3 value3)> And<T1, T2, T3>(in this Result<(T1 value1, T2 value2)> left, in Result<T3> right) =>
            All(left, right).Map(pair => (pair.value1.value1, pair.value1.value2, pair.value2));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<(T1 value1, T2 value2, T3 value3, T4 value4)> And<T1, T2, T3, T4>(in this Result<(T1 value1, T2 value2, T3 value3)> left, in Result<T4> right) =>
            All(left, right).Map(pair => (pair.value1.value1, pair.value1.value2, pair.value1.value3, pair.value2));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)> And<T1, T2, T3, T4, T5>(in this Result<(T1 value1, T2 value2, T3 value3, T4 value4)> left, in Result<T5> right) =>
            All(left, right).Map(pair => (pair.value1.value1, pair.value1.value2, pair.value1.value3, pair.value1.value4, pair.value2));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T1> Left<T1, T2>(in this Result<T1> left, in Result<T2> right) =>
            All(left, right).Map(pair => pair.value1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T2> Right<T1, T2>(in this Result<T1> left, in Result<T2> right) =>
            All(left, right).Map(pair => pair.value2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Return<TIn, TOut>(in this Result<TIn> result, TOut value)
        {
            if (result.IsSuccess()) return value;
            else if (result.TryFailure(out var failure)) return failure;
            else return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Flatten<T>(in this Result<Result<T>> result)
        {
            if (result.TryValue(out var value)) return value;
            else if (result.TryFailure(out var failure)) return failure;
            else return Failure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IResult Flatten<T>(in this Result<T> result) where T : IResult
        {
            if (result.TryValue(out var value)) return value;
            else if (result.TryFailure(out var failure)) return failure;
            else return Failure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Bind<TIn, TOut>(in this Result<TIn> result, Func<TIn, Result<TOut>> bind)
        {
            if (result.TryValue(out var value)) return bind(value);
            else if (result.TryFailure(out var failure)) return failure;
            else return Failure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Bind<TIn, TOut, TState>(in this Result<TIn> result, in TState state, Func<TIn, TState, Result<TOut>> bind)
        {
            if (result.TryValue(out var value)) return bind(value, state);
            else if (result.TryFailure(out var failure)) return failure;
            else return Failure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Recover<T>(in this Result<T> result, Func<string[], Result<T>> recover) =>
            result.TryMessages(out var messages) ? recover(messages) : result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<(T1 value1, T2 value2)> All<T1, T2>(in Result<T1> result1, in Result<T2> result2)
        {
            if (result1.TryValue(out var value1) && result2.TryValue(out var value2)) return (value1, value2);
            return Failure(result1.Messages().Concat(result2.Messages()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<(T1 value1, T2 value2, T3 value3)> All<T1, T2, T3>(in Result<T1> result1, in Result<T2> result2, in Result<T3> result3)
        {
            if (result1.TryValue(out var value1) && result2.TryValue(out var value2) && result3.TryValue(out var value3)) return (value1, value2, value3);
            return Failure(result1.Messages().Concat(result2.Messages()).Concat(result3.Messages()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<(T1 value1, T2 value2, T3 value3, T4 value4)> All<T1, T2, T3, T4>(in Result<T1> result1, in Result<T2> result2, in Result<T3> result3, in Result<T4> result4)
        {
            if (result1.TryValue(out var value1) && result2.TryValue(out var value2) && result3.TryValue(out var value3) && result4.TryValue(out var value4)) return (value1, value2, value3, value4);
            return Failure(result1.Messages().Concat(result2.Messages()).Concat(result3.Messages()).Concat(result4.Messages()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)> All<T1, T2, T3, T4, T5>(in Result<T1> result1, in Result<T2> result2, in Result<T3> result3, in Result<T4> result4, in Result<T5> result5)
        {
            if (result1.TryValue(out var value1) && result2.TryValue(out var value2) && result3.TryValue(out var value3) && result4.TryValue(out var value4) && result5.TryValue(out var value5)) return (value1, value2, value3, value4, value5);
            return Failure(result1.Messages().Concat(result2.Messages()).Concat(result3.Messages()).Concat(result4.Messages()).Concat(result5.Messages()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T[]> All<T>(params Result<T>[] results)
        {
            var values = new T[results.Length];
            var messages = new List<string>(results.Length);

            for (var i = 0; i < results.Length; i++)
            {
                var result = results[i];
                if (result.TryValue(out var value)) values[i] = value;
                else if (results[i].TryMessages(out var current)) messages.AddRange(current);
            }

            if (messages.Count == 0) return values;
            return Failure(messages.ToArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T[]> All<T>(this IEnumerable<Result<T>> results) => All(results.ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit> All(this IEnumerable<Result<Unit>> results) => results.All<Unit>().Return(default(Unit));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit> All(this IEnumerable<Failure> failures) => failures.Select(failure => failure.AsResult()).All();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Any<T>(this Result<T>[] results) => results.AsEnumerable().Any();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Any<T>(this IEnumerable<Result<T>> results)
        {
            var messages = new List<string>();

            foreach (var result in results)
            {
                if (result.TrySuccess(out var success)) return success;
                else if (result.TryMessages(out var current)) messages.AddRange(current);
            }

            return Failure(messages.ToArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit> Any(this IEnumerable<Result<Unit>> results) => results.Any<Unit>().Return(default(Unit));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> FirstOrFailure<T>(this IEnumerable<T> source)
        {
            foreach (var item in source) return item;
            return Failure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> FirstOrFailure<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (var item in source) if (predicate(item)) return item;
            return Failure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> LastOrFailure<T>(this IEnumerable<T> source)
        {
            var result = Failure().AsResult<T>();
            foreach (var item in source) result = item;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> LastOrFailure<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var result = Failure().AsResult<T>();
            foreach (var item in source) if (predicate(item)) result = item;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> Choose<T>(params Result<T>[] results)
        {
            foreach (var result in results) if (result.TryValue(out var value)) yield return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> Choose<T>(this IEnumerable<Result<T>> results) => Choose(results.ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Cast<T>(object value) => Success(value).AsResult().Cast<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Cast<TIn, TOut>(in TIn value) => Success(value).AsResult().Cast<TOut>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> As<T>(in this Result<T> result, Type type, bool hierarchy = false, bool definition = false) => result.Bind(
            (type, hierarchy, definition),
            (value, state) => As(value, state.type, state.hierarchy, state.definition));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> As<T>(in T value, Type type, bool hierarchy = false, bool definition = false) =>
            TypeUtility.Is(value, type, hierarchy, definition) ? Result.Success(value).AsResult() :
            Result.Failure($"Expected value '{value?.ToString() ?? "null"}' to be of type '{type.FullFormat()}'.");
    }
}
