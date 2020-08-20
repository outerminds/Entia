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

    public readonly struct Failure : IResult
    {
        public readonly string[] Messages;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Failure(params string[] messages) { Messages = messages; }

        Result.Tags IResult.Tag => Result.Tags.Failure;
        Result<T> IResult.Cast<T>() => Result.Failure(Messages);

        public override int GetHashCode() => ArrayUtility.GetHashCode(Messages);
        public override string ToString() => $"{nameof(Result.Tags.Failure)}({string.Join(", ", Messages)})";
    }

    public readonly struct Result<T> : IResult, IEquatable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Result<T>(in T value) => new Result<T>(Result.Tags.Success, value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Result<T>(Failure failure) => new Result<T>(Result.Tags.Failure, default, failure.Messages);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(in Result<T> result) => result.Tag == Result.Tags.Success;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Option<T>(in Result<T> result) => result.TryValue(out var value) ? Option.From(value) : Option.None();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Result<T>(in Option<T> option) => option.TryValue(out var value) ? Result.Success(value) : Result.Failure();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Result<T> left, in T right) => left.TryValue(out var value) && EqualityComparer<T>.Default.Equals(value, right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Result<T> left, in T right) => !(left == right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in T left, in Result<T> right) => right == left;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in T left, in Result<T> right) => !(left == right);

        public Result.Tags Tag { get; }

        readonly T _value;
        readonly string[] _messages;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Result(Result.Tags tag, in T value, params string[] messages)
        {
            Tag = tag;
            _value = value;
            _messages = messages;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryValue(out T value)
        {
            value = _value;
            return Tag == Result.Tags.Success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryMessages(out string[] messages)
        {
            messages = _messages;
            return Tag == Result.Tags.Failure;
        }

        public Result<TTo> Cast<TTo>() => this.Bind(value => value is TTo casted ?
            Result.Success(casted) :
            Result.Failure($"Expected value '{value?.ToString() ?? "null"}' to be of type '{typeof(TTo)}'."));

        public bool Equals(T other) => this == other;
        public override bool Equals(object obj) => obj is T value && this == value;

        public override int GetHashCode() => Tag == Result.Tags.Success ?
            EqualityComparer<T>.Default.GetHashCode(_value) :
            Result.Failure(_messages).GetHashCode();

        public override string ToString() => Tag == Result.Tags.Success ?
            $"{nameof(Result.Tags.Success)}({_value})" :
            Result.Failure(_messages).ToString();
    }

    public static class Result
    {
        public enum Tags : byte { Failure, Success }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Success<T>(in T value) => value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit> Success() => Success(default(Unit));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure Failure(params string[] messages) => new Failure(messages);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure Failure(IEnumerable<string> messages) => Failure(messages.ToArray());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure Failure(Exception exception) => Failure(exception.ToString());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Try<T>(Func<T> @try, Action @finally = null)
        {
            try { return @try(); }
            catch (Exception exception) { return Failure(exception); }
            finally { @finally?.Invoke(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Try<TState, T>(in TState state, Func<TState, T> @try, Action<TState> @finally = null)
        {
            try { return @try(state); }
            catch (Exception exception) { return Failure(exception); }
            finally { @finally?.Invoke(state); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit> Try(Action @try, Action @finally = null)
        {
            try { @try(); return default(Unit); }
            catch (Exception exception) { return Failure(exception); }
            finally { @finally?.Invoke(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit> Try<TState>(in TState state, Action<TState> @try, Action<TState> @finally = null)
        {
            try { @try(state); return default(Unit); }
            catch (Exception exception) { return Failure(exception); }
            finally { @finally?.Invoke(state); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Use<TIn, TOut>(in this Result<TIn> result, Func<TIn, TOut> use) where TIn : IDisposable
        {
            if (result.TryValue(out var value)) using (value) return Try(value, use);
            return result.Fail();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit> Use<T>(in this Result<T> result, Action<T> use) where T : IDisposable
        {
            if (result.TryValue(out var value)) using (value) return Try(value, use);
            return result.Ignore();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(in this Result<T> result, Tags tag) => result.Tag == tag;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSuccess<T>(in this Result<T> result) => result.Is(Tags.Success);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFailure<T>(in this Result<T> result) => result.Is(Tags.Failure);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> AsResult<T>(in this T? value) where T : struct =>
            value is T casted ? Success(casted) :
            Failure($"Expected value of type '{typeof(T).FullFormat()}?' to not be 'null'.");
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> AsResult<T>(this Failure failure) => failure;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit> AsResult(this Failure failure) => failure;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> AsResult<T>(in this Option<T> option, params string[] messages) =>
            option.TryValue(out var value) ? Success(value) : Failure(messages);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> AsOption<T>(in this Result<T> result) => result;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? AsNullable<T>(in this Result<T> result) where T : struct => result.TryValue(out var value) ? (T?)value : null;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> AsResult<T>(in this Or<T, string[]> or) => or.MapRight(messages => Failure(messages)).AsResult();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> AsResult<T>(in this Or<T, string> or) => or.MapRight(message => Failure(message)).AsResult();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> AsResult<T>(in this Or<T, Failure> or) => or.Match(value => Success(value), failure => failure);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Or<T, Failure> AsOr<T>(in this Result<T> result) => result.Match(value => Core.Or.Left(value).AsOr<Failure>(), messages => Failure(messages));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string[] Messages<T>(in this Result<T> result) => result.TryMessages(out var messages) ? messages : Array.Empty<string>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure Fail<T>(in this Result<T> result, params string[] messages) => Failure(result.Messages().Append(messages));

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

        public static T OrThrow<T>(in this Result<T> result) => result.Or(() => throw new NullReferenceException());
        public static T OrDefault<T>(in this Result<T> result) => result.Or(default(T));
        public static Result<Unit> Ignore<T>(in this Result<T> result) => result.Map(_ => default(Unit));
        public static Result<object> Box<T>(in this Result<T> result) => result.Map(value => (object)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Map<TIn, TOut>(in this Result<TIn> result, Func<TIn, TOut> map)
        {
            if (result.TryValue(out var value)) return map(value);
            return result.Fail();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Map<TIn, TOut, TState>(in this Result<TIn> result, in TState state, Func<TIn, TState, TOut> map)
        {
            if (result.TryValue(out var value)) return map(value, state);
            return result.Fail();
        }

        public static Result<T> Filter<T>(in this Result<T> result, bool filter, params string[] messages) =>
            filter ? result : result.Fail(messages);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Filter<T>(in this Result<T> result, Func<T, bool> filter, params string[] messages)
        {
            if (result.TryValue(out var value)) return filter(value) ? result : Failure(messages);
            return result.Fail(messages);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Filter<T, TState>(in this Result<T> result, in TState state, Func<T, TState, bool> filter, params string[] messages)
        {
            if (result.TryValue(out var value)) return filter(value, state) ? result : Failure(messages);
            return result.Fail(messages);
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
            return left.Fail();
        }

        public static Result<(T1, T2)> And<T1, T2>(in this Result<T1> left, in Result<T2> right)
        {
            if (left.TryValue(out var value1) && right.TryValue(out var value2)) return (value1, value2);
            return Failure(ArrayUtility.Concatenate(left.Messages(), right.Messages()));
        }

        public static Result<(T1, T2, T3)> And<T1, T2, T3>(in this Result<(T1, T2)> left, in T3 right)
        {
            if (left.TryValue(out var value1)) return (value1.Item1, value1.Item2, right);
            return left.Fail();
        }

        public static Result<(T1, T2, T3)> And<T1, T2, T3>(in this Result<(T1, T2)> left, in Result<T3> right)
        {
            if (left.TryValue(out var value1) && right.TryValue(out var value2)) return (value1.Item1, value1.Item2, value2);
            return Failure(ArrayUtility.Concatenate(left.Messages(), right.Messages()));
        }

        public static Result<(T1, T2, T3, T4)> And<T1, T2, T3, T4>(in this Result<(T1, T2, T3)> left, in T4 right)
        {
            if (left.TryValue(out var value1)) return (value1.Item1, value1.Item2, value1.Item3, right);
            return left.Fail();
        }

        public static Result<(T1, T2, T3, T4)> And<T1, T2, T3, T4>(in this Result<(T1, T2, T3)> left, in Result<T4> right)
        {
            if (left.TryValue(out var value1) && right.TryValue(out var value2)) return (value1.Item1, value1.Item2, value1.Item3, value2);
            return Failure(ArrayUtility.Concatenate(left.Messages(), right.Messages()));
        }

        public static Result<(T1, T2, T3, T4, T5)> And<T1, T2, T3, T4, T5>(in this Result<(T1, T2, T3, T4)> left, in T5 right)
        {
            if (left.TryValue(out var value1)) return (value1.Item1, value1.Item2, value1.Item3, value1.Item4, right);
            return left.Fail();
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

        public static Result<TOut> Return<TIn, TOut>(in this Result<TIn> result, TOut value)
        {
            if (result.IsSuccess()) return value;
            return result.Fail();
        }

        public static Result<T> Flatten<T>(in this Result<Result<T>> result)
        {
            if (result.TryValue(out var value)) return value;
            return result.Fail();
        }

        public static IResult Flatten<T>(in this Result<T> result) where T : IResult
        {
            if (result.TryValue(out var value)) return value;
            return result.Fail();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Bind<TIn, TOut>(in this Result<TIn> result, Func<TIn, Result<TOut>> bind)
        {
            if (result.TryValue(out var value)) return bind(value);
            return result.Fail();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TOut> Bind<TIn, TOut, TState>(in this Result<TIn> result, in TState state, Func<TIn, TState, Result<TOut>> bind)
        {
            if (result.TryValue(out var value)) return bind(value, state);
            return result.Fail();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Recover<T>(in this Result<T> result, Func<string[], Result<T>> recover) =>
            result.TryMessages(out var messages) ? recover(messages) : result;

        public static Result<T[]> All<T>(this Result<T>[] results)
        {
            var values = new T[results.Length];
            var messages = new List<string>(results.Length);
            var success = true;
            for (var i = 0; i < results.Length; i++)
            {
                var result = results[i];
                if (result.TryValue(out values[i])) continue;

                success = false;
                messages.AddRange(result.Messages());
            }
            return success ? Success(values) : Failure(messages.ToArray());
        }

        public static Result<T[]> All<T>(this IEnumerable<Result<T>> results) => results.ToArray().All();

        public static Result<Unit> All(this Result<Unit>[] results)
        {
            var messages = new List<string>(results.Length);
            var success = true;
            for (var i = 0; i < results.Length; i++)
            {
                var result = results[i];
                if (result.IsSuccess()) continue;

                success = false;
                messages.AddRange(result.Messages());
            }
            return success ? Success() : Failure(messages.ToArray());
        }

        public static Result<Unit> All(this IEnumerable<Result<Unit>> results) => results.ToArray().All();

        public static Result<T> Any<T>(this Result<T>[] results)
        {
            var messages = new List<string>(results.Length);
            foreach (var result in results)
            {
                if (result.TryValue(out var value)) return value;
                messages.AddRange(result.Messages());
            }
            return Failure(messages.ToArray());
        }

        public static Result<T> Any<T>(this IEnumerable<Result<T>> results) => results.ToArray().Any();
        public static Result<Unit> Any(this Result<Unit>[] results) => results.Any<Unit>().Return(default(Unit));
        public static Result<Unit> Any(this IEnumerable<Result<Unit>> results) => results.ToArray().Any();

        public static IEnumerable<T> Choose<T>(this Result<T>[] results)
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

        public static Result<T> FirstOrFailure<T>(this T[] source)
        {
            if (source.Length > 0) return source[0];
            return Failure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> FirstOrFailure<T>(this T[] source, Func<T, bool> predicate)
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

        public static Result<T> LastOrFailure<T>(this T[] source)
        {
            if (source.Length > 0) return source[source.Length - 1];
            return Failure();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> LastOrFailure<T>(this T[] source, Func<T, bool> predicate)
        {
            for (int i = source.Length - 1; i >= 0; i--)
            {
                var item = source[i];
                if (predicate(item)) return item;
            }
            return Failure();
        }

        public static Result<T> Cast<T>(object value) => Success(value).Cast<T>();
        public static Result<TOut> Cast<TIn, TOut>(in TIn value) => Success(value).Cast<TOut>();

        public static Result<T> As<T>(in this Result<T> result, Type type, bool hierarchy = false, bool definition = false) => result.Bind(
            (type, hierarchy, definition),
            (value, state) => As(value, state.type, state.hierarchy, state.definition));

        public static Result<T> As<T>(in T value, Type type, bool hierarchy = false, bool definition = false) =>
            TypeUtility.Is(value, type, hierarchy, definition) ? Success(value) :
            Failure($"Expected value '{value?.ToString() ?? "null"}' to be of type '{type.FullFormat()}'.");
    }
}
