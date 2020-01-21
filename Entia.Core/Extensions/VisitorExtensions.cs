using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Entia.Core
{
    public static class VisitorExtensions
    {
        public static bool Add<T, TIn, TState, TOut>(this Visitor<TIn, TState, TOut> visitor, Visit<T, TOut> visit) where T : TIn =>
            visitor.Add((T value, in TState state) => visit(value));

        public static Result<TOut> Visit<TIn, TOut>(this Visitor<TIn, Unit, TOut> visitor, TIn value) => visitor.Visit(value, default);

        public static Visitor<TIn, TState, TOut> Memoize<TIn, TState, TOut>(this Visitor<TIn, TState, TOut> visitor, IEqualityComparer<(TIn value, TState state)> comparer = null)
        {
            var memoized = new Visitor<TIn, TState, TOut>();
            foreach (var pair in visitor) memoized.Add(pair.type, pair.value.Memoize(comparer));
            return memoized;
        }

        public static Visitor<TIn, Unit, TOut> Memoize<TIn, TOut>(this Visitor<TIn, Unit, TOut> visitor, IEqualityComparer<TIn> comparer = null)
        {
            var memoized = new Visitor<TIn, Unit, TOut>();
            foreach (var pair in visitor) memoized.Add(pair.type, pair.value.Memoize(comparer));
            return memoized;
        }

        public static Visit<TIn, TOut> Memoize<TIn, TOut>(this Visit<TIn, TOut> visit, IEqualityComparer<TIn> comparer = null)
        {
            var cache = new ConcurrentDictionary<TIn, Result<TOut>>(comparer);
            return value => cache.TryGetValue(value, out var result) ? result : cache[value] = visit(value);
        }

        public static Visit<TIn, TState, TOut> Memoize<TIn, TState, TOut>(this Visit<TIn, TState, TOut> visit, IEqualityComparer<(TIn value, TState state)> comparer = null)
        {
            var cache = new ConcurrentDictionary<(TIn value, TState state), Result<TOut>>(comparer);
            return (TIn value, in TState state) =>
                cache.TryGetValue((value, state), out var result) ? result :
                cache[(value, state)] = visit(value, state);
        }

        public static Visit<TIn, Unit, TOut> Memoize<TIn, TOut>(this Visit<TIn, Unit, TOut> visit, IEqualityComparer<TIn> comparer = null)
        {
            var cache = new ConcurrentDictionary<TIn, Result<TOut>>(comparer);
            return (TIn value, in Unit _) =>
                cache.TryGetValue(value, out var result) ? result :
                cache[value] = visit(value, default);
        }
    }
}