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

    public readonly struct Success<T> : IResult, IEquatable<Success<T>>
    {
        public static bool operator ==(in Success<T> left, in T right) => EqualityComparer<T>.Default.Equals(left.Value, right);
        public static bool operator !=(in Success<T> left, in T right) => !(left == right);
        public static bool operator ==(in T left, in Success<T> right) => right == left;
        public static bool operator !=(in T left, in Success<T> right) => !(left == right);
        public static bool operator ==(in Success<T> left, in Success<T> right) => right == left.Value;
        public static bool operator !=(in Success<T> left, in Success<T> right) => !(left == right);

        public static implicit operator Success<T>(in T value) => new Success<T>(value);
        public static implicit operator Some<T>(in Success<T> success) => success.Value;
        public static implicit operator Option<T>(in Success<T> success) => success.Value;

        Result.Tags IResult.Tag => Result.Tags.Success;

        public readonly T Value;
        public Success(in T value) { Value = value; }

        Result<T1> IResult.Cast<T1>() => Result.Cast<T1>(Value);

        public bool Equals(Success<T> other) => EqualityComparer<T>.Default.Equals(Value, other.Value);
        public override bool Equals(object obj) =>
            obj is T value ? this == value :
            obj is Success<T> success ? this == success :
            false;

        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Value);
        public override string ToString() => $"{GetType().Format()}({Value})";
    }

    public readonly struct Failure : IResult, IEquatable<Failure>
    {
        public static bool operator ==(Failure left, Failure right) => true;
        public static bool operator !=(Failure left, Failure right) => !(left == right);
        public static implicit operator None(in Failure failure) => Option.None();
        public static implicit operator Failure(in None none) => Result.Failure();

        public readonly string[] Messages;
        public Failure(params string[] messages) { Messages = messages; }

        Result.Tags IResult.Tag => Result.Tags.Failure;
        Result<T> IResult.Cast<T>() => Result.Failure(Messages);

        public bool Equals(Failure other) => this == other;
        public override bool Equals(object obj) => obj is null || obj is Failure;
        public override int GetHashCode() => 0;
        public override string ToString() => $"{GetType().Format()}({string.Join(", ", Messages)})";
    }

    public readonly struct Result<T> : IResult, IEquatable<Success<T>>, IEquatable<T>
    {
        static readonly Func<T, bool> _isNull = typeof(T).IsValueType ?
            new Func<T, bool>(_ => false) : new Func<T, bool>(value => value == null);

        public static implicit operator Result<T>(in Success<T> success) => new Result<T>(Result.Tags.Success, success.Value);
        public static implicit operator Result<T>(in T value) => new Result<T>(Result.Tags.Success, value);
        public static implicit operator Result<T>(in Failure failure) => new Result<T>(Result.Tags.Failure, default, failure.Messages);
        public static implicit operator bool(in Result<T> result) => result.Tag == Result.Tags.Success;
        public static explicit operator Success<T>(in Result<T> result) => result.Tag == Result.Tags.Success ? new Success<T>(result._value) : throw new InvalidCastException();
        public static explicit operator Failure(in Result<T> result) => result.Tag == Result.Tags.Failure ? new Failure(result._messages) : throw new InvalidCastException();
        public static implicit operator Option<T>(in Result<T> result) => result.TryValue(out var value) ? Option.From(value) : Option.None();
        public static implicit operator Result<T>(in Option<T> option) => option.TryValue(out var value) ? Result.Success(value).AsResult() : Result.Failure();

        public static bool operator ==(in Result<T> left, in T right) => left.TryValue(out var value) && EqualityComparer<T>.Default.Equals(value, right);
        public static bool operator !=(in Result<T> left, in T right) => !(left == right);
        public static bool operator ==(in T left, in Result<T> right) => right == left;
        public static bool operator !=(in T left, in Result<T> right) => !(left == right);
        public static bool operator ==(in Result<T> left, in Success<T> right) => left == right.Value;
        public static bool operator !=(in Result<T> left, in Success<T> right) => !(left == right);
        public static bool operator ==(in Success<T> left, in Result<T> right) => right == left.Value;
        public static bool operator !=(in Success<T> left, in Result<T> right) => !(left == right);

        public Result.Tags Tag { get; }

        readonly T _value;
        readonly string[] _messages;

        Result(Result.Tags tag, in T value, params string[] messages)
        {
            Tag = tag;
            _value = value;
            _messages = messages;
        }

        public Result<TTo> Cast<TTo>() => this.Bind(value => value is TTo casted ?
            Result.Success(casted).AsResult() :
            Result.Failure($"Expected value '{value?.ToString() ?? "null"}' to be of type '{typeof(TTo)}'."));

        public bool Equals(T other) => this == other;
        public bool Equals(Success<T> other) => this == other;
        public override bool Equals(object obj) =>
            obj is T value ? this == value :
            obj is Success<T> success ? this == success :
            false;

        public override int GetHashCode() =>
            this.TryValue(out var value) ? EqualityComparer<T>.Default.GetHashCode(value) :
            this.TryMessages(out var messages) ? ArrayUtility.GetHashCode(_messages) :
            0;

        public override string ToString()
        {
            return
                this.TrySuccess(out var success) ? success.ToString() :
                this.TryFailure(out var failure) ? failure.ToString() :
                base.ToString();
        }
    }

    public static class Result
    {
        public enum Tags : byte { Failure, Success }

        public static Success<T> Success<T>(in T value) => new Success<T>(value);
        public static Success<Unit> Success() => new Success<Unit>(default);
        public static Failure Failure(params string[] messages) => new Failure(messages);
        public static Failure Failure(IEnumerable<string> messages) => Failure(messages.ToArray());
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
            catch (Exception exception) { return Exception(exception); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit> Try(Action @try)
        {
            try { @try(); return default(Unit); }
            catch (Exception exception) { return Exception(exception); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit> Try<TState>(in TState input, Action<TState> @try)
        {
            try { @try(input); return default(Unit); }
            catch (Exception exception) { return Exception(exception); }
        }

        public static bool Is<T>(in this Result<T> result, Tags tag) => result.Tag == tag;
        public static bool IsSuccess<T>(in this Result<T> result) => result.Is(Tags.Success);
        public static bool IsFailure<T>(in this Result<T> result) => result.Is(Tags.Failure);
        public static Result<T> AsResult<T>(in this Success<T> success) => success;
        public static Result<T> AsResult<T>(in this Failure failure) => failure;
        public static Result<Unit> AsResult(in this Failure failure) => failure;
        public static Success<T> AsResult<T>(in this Some<T> some) => some.Value;
        public static Failure AsResult(in this None none, params string[] messages) => new Failure(messages);
        public static Result<T> AsResult<T>(in this Option<T> option, params string[] messages) =>
            option.TryValue(out var value) ? Success(value).AsResult() : Failure(messages);
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

        public static string[] Messages<T>(in this Result<T> result) => result.TryMessages(out var messages) ? messages : Array.Empty<string>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Success<TOut> Map<TIn, TOut>(in this Success<TIn> success, Func<TIn, TOut> map) => map(success.Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Success<TOut> Map<TIn, TOut, TState>(in this Success<TIn> success, in TState state, Func<TIn, TState, TOut> map) => map(success.Value, state);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Filter<T>(in this Success<T> success, Func<T, bool> filter) => filter(success.Value) ? success.AsResult() : Failure();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Filter<T, TState>(in this Success<T> success, in TState state, Func<T, TState, bool> filter) => filter(success.Value, state) ? success.AsResult() : Failure();
        public static T Flatten<T>(in this Success<T> success) where T : IResult => success.Value;
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
        public static T Or<T>(in this Result<T> result, Func<T> provide) =>
            result.TryValue(out var current) ? current : provide();

        public static T Or<T>(in this Result<T> result, in T value) => result.TryValue(out var current) ? current : value;
        public static Result<T> Or<T>(in this Result<T> result1, in Result<T> result2) => result1.TryValue(out var value1) ? value1 : result2;
        public static Result<T> Or<T>(in this Result<T> result1, in Result<T> result2, in Result<T> result3) => result1.Or(result2).Or(result3);
        public static Result<T> Or<T>(in this Result<T> result1, in Result<T> result2, in Result<T> result3, in Result<T> result4) => result1.Or(result2).Or(result3).Or(result4);
        public static Result<T> Or<T>(in this Result<T> result1, in Result<T> result2, in Result<T> result3, in Result<T> result4, in Result<T> result5) => result1.Or(result2).Or(result3).Or(result4).Or(result5);

        public static T OrDefault<T>(in this Result<T> result) => result.Or(default(T));
        public static Success<Unit> Ignore<T>(in this Success<T> success) => success.Map(_ => default(Unit));
        public static Result<Unit> Ignore<T>(in this Result<T> result) => result.Map(_ => default(Unit));
        public static Result<object> Box<T>(this T result) where T : IResult => result.Cast<object>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Map<TIn, TOut>(in this Result<TIn> result, Func<TIn, TOut> map)
        {
            if (result.TryValue(out var value)) return map(value);
            return result.AsFailure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Map<TIn, TOut, TState>(in this Result<TIn> result, in TState state, Func<TIn, TState, TOut> map)
        {
            if (result.TryValue(out var value)) return map(value, state);
            return result.AsFailure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Filter<T>(in this Result<T> result, Func<T, bool> filter)
        {
            if (result.TryValue(out var value)) return filter(value) ? result : Failure();
            return result.AsFailure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Filter<T, TState>(in this Result<T> result, in TState state, Func<T, TState, bool> filter)
        {
            if (result.TryValue(out var value)) return filter(value, state) ? result : Failure();
            return result.AsFailure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Fold<TIn, TOut>(in this Result<TIn> result, in TOut seed, Func<TOut, TIn, TOut> fold)
        {
            if (result.TryValue(out var value)) return fold(seed, value);
            return seed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Fold<TIn, TOut, TState>(in this Result<TIn> result, in TOut seed, in TState state, Func<TOut, TIn, TState, TOut> fold)
        {
            if (result.TryValue(out var value)) return fold(seed, value, state);
            return seed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Match<TIn, TOut>(in this Result<TIn> result, Func<TIn, TOut> success, Func<string[], TOut> failure) =>
            result.TryValue(out var value) ? success(value) : failure(result.Messages());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Match<T>(in this Result<T> result, Action<T> success, Action<string[]> failure)
        {
            if (result.TryValue(out var value)) success(value);
            else failure(result.Messages());
            return result;
        }

        public static Result<(T1, T2)> And<T1, T2>(in this Result<T1> left, in T2 right)
        {
            if (left.TryValue(out var value1)) return (value1, right);
            return left.AsFailure();
        }

        public static Result<(T1, T2)> And<T1, T2>(in this Result<T1> left, in Result<T2> right)
        {
            if (left.TryValue(out var value1) && right.TryValue(out var value2)) return (value1, value2);
            return Failure(ArrayUtility.Concatenate(left.Messages(), right.Messages()));
        }

        public static Result<(T1, T2, T3)> And<T1, T2, T3>(in this Result<(T1, T2)> left, in T3 right)
        {
            if (left.TryValue(out var value1)) return (value1.Item1, value1.Item2, right);
            return left.AsFailure();
        }

        public static Result<(T1, T2, T3)> And<T1, T2, T3>(in this Result<(T1, T2)> left, in Result<T3> right)
        {
            if (left.TryValue(out var value1) && right.TryValue(out var value2)) return (value1.Item1, value1.Item2, value2);
            return Failure(ArrayUtility.Concatenate(left.Messages(), right.Messages()));
        }

        public static Result<(T1, T2, T3, T4)> And<T1, T2, T3, T4>(in this Result<(T1, T2, T3)> left, in T4 right)
        {
            if (left.TryValue(out var value1)) return (value1.Item1, value1.Item2, value1.Item3, right);
            return left.AsFailure();
        }

        public static Result<(T1, T2, T3, T4)> And<T1, T2, T3, T4>(in this Result<(T1, T2, T3)> left, in Result<T4> right)
        {
            if (left.TryValue(out var value1) && right.TryValue(out var value2)) return (value1.Item1, value1.Item2, value1.Item3, value2);
            return Failure(ArrayUtility.Concatenate(left.Messages(), right.Messages()));
        }

        public static Result<(T1, T2, T3, T4, T5)> And<T1, T2, T3, T4, T5>(in this Result<(T1, T2, T3, T4)> left, in T5 right)
        {
            if (left.TryValue(out var value1)) return (value1.Item1, value1.Item2, value1.Item3, value1.Item4, right);
            return left.AsFailure();
        }

        public static Result<(T1, T2, T3, T4, T5)> And<T1, T2, T3, T4, T5>(in this Result<(T1, T2, T3, T4)> left, in Result<T5> right)
        {
            if (left.TryValue(out var value1) && right.TryValue(out var value2)) return (value1.Item1, value1.Item2, value1.Item3, value1.Item4, value2);
            return Failure(ArrayUtility.Concatenate(left.Messages(), right.Messages()));
        }

        public static Result<(T1, T2, T3)> And<T1, T2, T3>(in this Result<T1> result1, in Result<T2> result2, in Result<T3> result3)
        {
            if (result1.TryValue(out var value1) && result2.TryValue(out var value2) && result3.TryValue(out var value3)) return (value1, value2, value3);
            return Failure(ArrayUtility.Concatenate(result1.Messages(), result2.Messages(), result3.Messages()));
        }

        public static Result<(T1, T2, T3, T4)> And<T1, T2, T3, T4>(in this Result<T1> result1, in Result<T2> result2, in Result<T3> result3, in Result<T4> result4)
        {
            if (result1.TryValue(out var value1) && result2.TryValue(out var value2) && result3.TryValue(out var value3) && result4.TryValue(out var value4)) return (value1, value2, value3, value4);
            return Failure(ArrayUtility.Concatenate(result1.Messages(), result2.Messages(), result3.Messages(), result4.Messages()));
        }

        public static Result<(T1, T2, T3, T4, T5)> And<T1, T2, T3, T4, T5>(in this Result<T1> result1, in Result<T2> result2, in Result<T3> result3, in Result<T4> result4, in Result<T5> result5)
        {
            if (result1.TryValue(out var value1) && result2.TryValue(out var value2) && result3.TryValue(out var value3) && result4.TryValue(out var value4) && result5.TryValue(out var value5)) return (value1, value2, value3, value4, value5);
            return Failure(ArrayUtility.Concatenate(result1.Messages(), result2.Messages(), result3.Messages(), result4.Messages(), result5.Messages()));
        }

        public static Result<T1> Left<T1, T2>(in this Result<(T1, T2)> result) => result.Map(pair => pair.Item1);
        public static Result<T2> Right<T1, T2>(in this Result<(T1, T2)> result) => result.Map(pair => pair.Item2);

        public static Result<TOut> Return<TIn, TOut>(in this Result<TIn> result, TOut value)
        {
            if (result.IsSuccess()) return value;
            return result.AsFailure();
        }

        public static Result<T> Flatten<T>(in this Result<Result<T>> result)
        {
            if (result.TryValue(out var value)) return value;
            return result.AsFailure();
        }

        public static IResult Flatten<T>(in this Result<T> result) where T : IResult
        {
            if (result.TryValue(out var value)) return value;
            return result.AsFailure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Bind<TIn, TOut>(in this Result<TIn> result, Func<TIn, Result<TOut>> bind)
        {
            if (result.TryValue(out var value)) return bind(value);
            return result.AsFailure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Bind<TIn, TOut, TState>(in this Result<TIn> result, in TState state, Func<TIn, TState, Result<TOut>> bind)
        {
            if (result.TryValue(out var value)) return bind(value, state);
            return result.AsFailure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Recover<T>(in this Result<T> result, Func<string[], Result<T>> recover) =>
            result.TryMessages(out var messages) ? recover(messages) : result;

        public static Result<T[]> All<T>(params Result<T>[] results)
        {
            var values = new T[results.Length];
            var messages = new List<string>(results.Length);
            var success = true;
            for (var i = 0; i < results.Length; i++)
            {
                var result = results[i];
                if (result.TryValue(out var value)) values[i] = value;
                else if (results[i].TryMessages(out var current))
                {
                    success = false;
                    messages.AddRange(current);
                }
            }
            return success ? Success(values).AsResult() : Failure(messages.ToArray());
        }

        public static Result<T[]> All<T>(this IEnumerable<Result<T>> results) => All(results.ToArray());
        public static Result<Unit> All(this IEnumerable<Result<Unit>> results) => results.All<Unit>().Return(default(Unit));
        public static Result<T> Any<T>(this Result<T>[] results) => results.AsEnumerable().Any();

        public static Result<T> Any<T>(this IEnumerable<Result<T>> results)
        {
            var messages = new List<string>();
            foreach (var result in results)
            {
                if (result.TryValue(out var value)) return value;
                messages.AddRange(result.Messages());
            }
            return Failure(messages.ToArray());
        }

        public static Result<Unit> Any(this IEnumerable<Result<Unit>> results) => results.Any<Unit>().Return(default(Unit));

        public static IEnumerable<T> Choose<T>(params Result<T>[] results)
        {
            foreach (var result in results) if (result.TryValue(out var value)) yield return value;
        }

        public static IEnumerable<T> Choose<T>(this IEnumerable<Result<T>> results)
        {
            foreach (var result in results) if (result.TryValue(out var value)) yield return value;
        }

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

        public static Result<T> Cast<T>(object value) => Success(value).AsResult().Cast<T>();
        public static Result<TOut> Cast<TIn, TOut>(in TIn value) => Success(value).AsResult().Cast<TOut>();

        public static Result<T> As<T>(in this Result<T> result, Type type, bool hierarchy = false, bool definition = false) => result.Bind(
            (type, hierarchy, definition),
            (value, state) => As(value, state.type, state.hierarchy, state.definition));

        public static Result<T> As<T>(in T value, Type type, bool hierarchy = false, bool definition = false) =>
            TypeUtility.Is(value, type, hierarchy, definition) ? Result.Success(value).AsResult() :
            Result.Failure($"Expected value '{value?.ToString() ?? "null"}' to be of type '{type.FullFormat()}'.");
    }
}