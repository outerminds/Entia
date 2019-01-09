using System;
using System.Collections.Generic;
using System.Linq;

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

        public readonly T Value;
        public Success(in T value) { Value = value; }

        Result.Tags IResult.Tag => Result.Tags.Success;
        Result<T1> IResult.Cast<T1>() => Result.Cast<T1>(Value);

        public static implicit operator Success<T>(in T value) => new Success<T>(value);
        public static implicit operator Success<Unit>(in Success<T> _) => default(Unit);
        public static implicit operator Some<T>(in Success<T> success) => success.Value;
        public static implicit operator Some<Unit>(in Success<T> _) => default(Unit);
        public static implicit operator Option<T>(in Success<T> success) => success.Value;
        public static implicit operator Option<Unit>(in Success<T> _) => default(Unit);
    }

    public readonly struct Failure : IResult
    {
        public static readonly Failure Empty = new Failure(new string[0]);

        public readonly string[] Messages;
        public Failure(string[] messages) { Messages = messages; }

        Result.Tags IResult.Tag => Result.Tags.Failure;
        Result<T> IResult.Cast<T>() => Result.Failure(Messages);

        public static implicit operator None(in Failure failure) => Option.None();
        public static implicit operator Failure(in None none) => Empty;
    }

    public readonly struct Result<T> : IResult
    {
        public Result.Tags Tag { get; }

        readonly T _value;
        readonly string[] _messages;

        public Result<TTo> Cast<TTo>() => this.Bind(value =>
            value is TTo casted ? Result.Success(casted).AsResult() :
            Result.Failure($"Expected value '{value?.ToString() ?? "null"}' to be of type '{typeof(TTo)}'."));

        Result(Result.Tags tag, in T value, string[] messages)
        {
            Tag = tag;
            _value = value;
            _messages = messages;
        }

        public static implicit operator Result<T>(in T value) => Result.Success(value);

        public static implicit operator Result<T>(in Success<T> success) => new Result<T>(Result.Tags.Success, success.Value, null);
        public static implicit operator Option<T>(in Result<T> result) => result.TryValue(out var value) ? Option.Some(value).AsOption() : Option.None();
        public static implicit operator Result<T>(in Option<T> option) => option.TryValue(out var value) ? Result.Success(value).AsResult() : Result.Failure();
        public static explicit operator Success<T>(in Result<T> result) => result.Tag == Result.Tags.Success ?
            Result.Success(result._value) : throw new InvalidCastException();

        public static implicit operator Result<T>(in Failure failure) => new Result<T>(Result.Tags.Failure, default, failure.Messages);
        public static explicit operator Failure(in Result<T> result) => result.Tag == Result.Tags.Failure ?
            Result.Failure(result._messages) : throw new InvalidCastException();

        public static implicit operator Result<Unit>(in Result<T> result) => result.Map(_ => default(Unit));
    }

    public static class Result
    {
        public enum Tags : byte { None, Success, Failure }

        public static Success<T> Success<T>(in T value) => new Success<T>(value);
        public static Success<Unit> Success() => new Success<Unit>(default);
        public static Failure Failure() => Core.Failure.Empty;
        public static Failure Failure(params string[] messages) => new Failure(messages);
        public static Failure Failure(IEnumerable<string> messages) => Failure(messages.ToArray());

        public static Failure Exception(Exception exception) => Failure(exception.ToString());

        public static Result<T> Try<T>(Func<T> @try)
        {
            try { return @try(); }
            catch (Exception exception) { return Exception(exception); }
        }

        public static Result<TOut> Try<TIn, TOut>(Func<TIn, TOut> @try, TIn input)
        {
            try { return @try(input); }
            catch (Exception exception) { return Failure(exception.ToString()); }
        }

        public static Result<TOut> Try<TIn1, TIn2, TOut>(Func<TIn1, TIn2, TOut> @try, TIn1 input1, TIn2 input2)
        {
            try { return @try(input1, input2); }
            catch (Exception exception) { return Failure(exception.ToString()); }
        }

        public static Result<Unit> Try(Action @try)
        {
            try { @try(); return default(Unit); }
            catch (Exception exception) { return Failure(exception.ToString()); }
        }

        public static Result<Unit> Try<T>(Action<T> @try, T input)
        {
            try { @try(input); return default(Unit); }
            catch (Exception exception) { return Failure(exception.ToString()); }
        }

        public static Result<Unit> Try<T1, T2>(Action<T1, T2> @try, T1 input1, T2 input2)
        {
            try { @try(input1, input2); return default(Unit); }
            catch (Exception exception) { return Failure(exception.ToString()); }
        }

        public static bool Is<T>(in this Result<T> result, Tags tag) => result.Tag == tag;
        public static bool IsSuccess<T>(in this Result<T> result) => result.Is(Tags.Success);
        public static bool IsFailure<T>(in this Result<T> result) => result.Is(Tags.Failure);
        public static Result<T> AsResult<T>(in this Success<T> success) => success;
        public static Result<T> AsResult<T>(in this Failure failure) => failure;
        public static Result<Unit> AsResult(in this Failure failure) => failure;
        public static Result<T> AsResult<T>(in this Option<T> option) => option;
        public static Option<T> AsOption<T>(in this Result<T> result) => result;
        public static Failure AsFailure<T>(in this Result<T> result) => result.TryFailure(out var failure) ? failure : Failure();

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

        public static IEnumerable<string> Messages<T>(in this Result<T> result) => result.TryMessages(out var messages) ? messages : new string[0];
        public static Success<TOut> Map<TIn, TOut>(in this Success<TIn> success, Func<TIn, TOut> map) => map(success.Value);
        public static Success<T> Flatten<T>(in this Success<Success<T>> success) => success.Value;
        public static Failure Flatten(in this Success<Failure> success) => success.Value;
        public static Result<T> Flatten<T>(in this Success<Result<T>> success) => success.Value;
        public static Result<TOut> Bind<TIn, TOut>(in this Success<TIn> success, Func<TIn, Result<TOut>> bind) => bind(success.Value);
        public static Success<TOut> Bind<TIn, TOut>(in this Success<TIn> success, Func<TIn, Success<TOut>> bind) => bind(success.Value);
        public static Failure Bind<T>(in this Success<T> success, Func<T, Failure> bind) => bind(success.Value);

        public static Result<T> Do<T>(in this Result<T> result, Action<T> @do)
        {
            if (result.TryValue(out var value)) @do(value);
            return result;
        }

        public static Result<T> Do<T, TState>(in this Result<T> result, Action<T, TState> @do, TState state)
        {
            if (result.TryValue(out var value)) @do(value, state);
            return result;
        }

        public static T Or<T>(in this Result<T> result, Func<T> provide) => result.TryValue(out var current) ? current : provide();

        public static T Or<T>(in this Result<T> result, in T value) => result.TryValue(out var current) ? current : value;

        public static Success<Unit> Ignore<T>(in this Success<T> success) => success;

        public static Result<Unit> Ignore<T>(in this Result<T> result) => result;

        public static Result<object> Box<T>(this T result) where T : IResult => result.Cast<object>();

        public static Result<TOut> Map<TIn, TOut>(in this Result<TIn> result, Func<TIn, TOut> map)
        {
            if (result.TryValue(out var value)) return map(value);
            else if (result.TryFailure(out var failure)) return failure;
            else return Failure();
        }

        public static Result<TOut> Map<TIn, TOut, TState>(in this Result<TIn> result, Func<TIn, TState, TOut> map, in TState state)
        {
            if (result.TryValue(out var value)) return map(value, state);
            else if (result.TryFailure(out var failure)) return failure;
            else return Failure();
        }

        public static TOut Match<TIn, TOut>(in this Result<TIn> result, Func<TIn, TOut> success, Func<string[], TOut> failure)
        {
            if (result.TryValue(out var value)) return success(value);
            else if (result.TryMessages(out var messages)) return failure(messages);
            else return default;
        }

        public static Result<T> Match<T>(in this Result<T> result, Action<T> success, Action<string[]> failure)
        {
            if (result.TryValue(out var value)) success(value);
            else if (result.TryMessages(out var messages)) failure(messages);
            return result;
        }

        public static Result<(T1 left, T2 right)> And<T1, T2>(in this Result<T1> left, in Result<T2> right) =>
            left.And(right, (a, b) => (a, b));

        public static Result<T3> And<T1, T2, T3>(in this Result<T1> left, in Result<T2> right, Func<T1, T2, T3> select)
        {
            if (left.TryValue(out var value1) && right.TryValue(out var value2)) return Success(select(value1, value2));
            else if (left.TryFailure(out var failure1))
                return right.TryFailure(out var failure2) ? Failure(failure1.Messages.Concat(failure2.Messages).ToArray()) : failure1;
            else if (right.TryFailure(out var failure2)) return failure2;
            else return Failure();
        }

        public static Result<T1> Left<T1, T2>(in this Result<T1> left, in Result<T2> right) => left.And(right, (a, _) => a);

        public static Result<T2> Right<T1, T2>(in this Result<T1> left, in Result<T2> right) => left.And(right, (_, b) => b);

        public static Result<TOut> Return<TIn, TOut>(in this Result<TIn> result, TOut value)
        {
            if (result.IsSuccess()) return value;
            else if (result.TryFailure(out var failure)) return failure;
            else return default;
        }

        public static Result<T> Flatten<T>(in this Result<Result<T>> result)
        {
            if (result.TryValue(out var value)) return value;
            else if (result.TryFailure(out var failure)) return failure;
            else return Failure();
        }

        public static IResult Flatten<T>(in this Result<T> result) where T : IResult
        {
            if (result.TryValue(out var value)) return value;
            else if (result.TryFailure(out var failure)) return failure;
            else return Failure();
        }

        public static Result<TOut> Bind<TIn, TOut>(in this Result<TIn> result, Func<TIn, Result<TOut>> bind)
        {
            if (result.TryValue(out var value)) return bind(value);
            else if (result.TryFailure(out var failure)) return failure;
            else return Failure();
        }

        public static Result<TOut> Bind<TIn, TOut, TState>(in this Result<TIn> result, Func<TIn, TState, Result<TOut>> bind, in TState state)
        {
            if (result.TryValue(out var value)) return bind(value, state);
            else if (result.TryFailure(out var failure)) return failure;
            else return Failure();
        }

        public static Result<T> Recover<T>(in this Result<T> result, Func<string[], Result<T>> recover) =>
            result.TryMessages(out var messages) ? recover(messages) : result;

        public static Result<(T1, T2)> All<T1, T2>(Result<T1> result1, Result<T2> result2)
        {
            if (result1.TryValue(out var value1) && result2.TryValue(out var value2)) return (value1, value2);
            return Failure(result1.Messages().Concat(result2.Messages()));
        }

        public static Result<(T1, T2, T3)> All<T1, T2, T3>(Result<T1> result1, Result<T2> result2, Result<T3> result3)
        {
            if (result1.TryValue(out var value1) && result2.TryValue(out var value2) && result3.TryValue(out var value3)) return (value1, value2, value3);
            return Failure(result1.Messages().Concat(result2.Messages()).Concat(result3.Messages()));
        }

        public static Result<(T1, T2, T3, T4)> All<T1, T2, T3, T4>(Result<T1> result1, Result<T2> result2, Result<T3> result3, Result<T4> result4)
        {
            if (result1.TryValue(out var value1) && result2.TryValue(out var value2) && result3.TryValue(out var value3) && result4.TryValue(out var value4)) return (value1, value2, value3, value4);
            return Failure(result1.Messages().Concat(result2.Messages()).Concat(result3.Messages()).Concat(result4.Messages()));
        }

        public static Result<T[]> All<T>(params Result<T>[] results)
        {
            var values = new T[results.Length];
            var messages = new List<string>();

            for (var i = 0; i < results.Length; i++)
            {
                var result = results[i];
                if (result.TryValue(out var value)) values[i] = value;
                else if (results[i].TryMessages(out var current)) messages.AddRange(current);
            }

            if (messages.Count == 0) return values;
            return Failure(messages.ToArray());
        }

        public static Result<T[]> All<T>(this IEnumerable<Result<T>> results) => All(results.ToArray());

        public static Result<Unit> All(this IEnumerable<Result<Unit>> results) => results.All<Unit>().Return(default(Unit));

        public static Result<Unit> All(this IEnumerable<Failure> failures) => failures.Select(failure => failure.AsResult()).All();

        public static Result<T> Any<T>(this Result<T>[] results) => results.AsEnumerable().Any();

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

        public static Result<Unit> Any(this IEnumerable<Result<Unit>> results) => results.Any<Unit>().Return(default(Unit));

        public static IEnumerable<T> Choose<T>(params Result<T>[] results)
        {
            foreach (var result in results) if (result.TryValue(out var value)) yield return value;
        }

        public static IEnumerable<T> Choose<T>(this IEnumerable<Result<T>> results) => Choose(results.ToArray());

        public static Result<T> Cast<T>(object value) => Success(value).AsResult().Cast<T>();

        public static Result<TOut> Cast<TIn, TOut>(in TIn value) => Success(value).AsResult().Cast<TOut>();

        public static Result<T> As<T>(in this Result<T> result, Type type, bool hierarchy = false, bool definition = false) => result.Bind(
            (value, state) =>
                value.Is(state.type, state.hierarchy, state.definition) ? Result.Success(value).AsResult() :
                Result.Failure($"Expected value '{value?.ToString() ?? "null"}' to be of type '{state}'."),
            (type, hierarchy, definition));
    }
}
