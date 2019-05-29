using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Core
{
    public interface IEnumerable<TEnumerator, TItem> : IEnumerable<TItem> where TEnumerator : IEnumerator<TItem>
    {
        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        new TEnumerator GetEnumerator();
    }

    public static class EnumerableExtensions
    {
        public static TAccumulate Aggregate<TEnumerator, TItem, TAccumulate>(this IEnumerable<TEnumerator, TItem> source, in TAccumulate seed, Func<TAccumulate, TItem, TAccumulate> aggregator) where TEnumerator : IEnumerator<TItem>
        {
            using (var enumerator = source.GetEnumerator())
            {
                var current = seed;
                while (enumerator.MoveNext()) current = aggregator(seed, enumerator.Current);
                return current;
            }
        }

        public static TAccumulate Aggregate<TEnumerator, TItem, TAccumulate>(this IEnumerable<TEnumerator, TItem> source, in TAccumulate seed, Func<TAccumulate, TItem, int, TAccumulate> aggregator) where TEnumerator : IEnumerator<TItem>
        {
            using (var enumerator = source.GetEnumerator())
            {
                var index = 0;
                var current = seed;
                while (enumerator.MoveNext()) current = aggregator(seed, enumerator.Current, index++);
                return current;
            }
        }

        public static bool Any<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source) where TEnumerator : IEnumerator<TItem>
        {
            using (var enumerator = source.GetEnumerator()) return enumerator.MoveNext();
        }

        public static bool Any<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, Func<TItem, bool> predicate) where TEnumerator : IEnumerator<TItem> =>
            source.Any(predicate, (current, _, state) => state(current));

        public static bool Any<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, Func<TItem, int, bool> predicate) where TEnumerator : IEnumerator<TItem> =>
            source.Any(predicate, (current, index, state) => state(current, index));

        public static bool Any<TEnumerator, TItem, TState>(this IEnumerable<TEnumerator, TItem> source, in TState state, Func<TItem, int, TState, bool> predicate) where TEnumerator : IEnumerator<TItem>
        {
            using (var enumerator = source.GetEnumerator())
            {
                var index = 0;
                while (enumerator.MoveNext()) if (predicate(enumerator.Current, index++, state)) return true;
                return false;
            }
        }

        public static bool None<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source) where TEnumerator : IEnumerator<TItem> =>
            !source.Any();

        public static bool None<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, Func<TItem, bool> predicate) where TEnumerator : IEnumerator<TItem> =>
            !source.Any(predicate);

        public static bool None<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, Func<TItem, int, bool> predicate) where TEnumerator : IEnumerator<TItem> =>
            !source.Any(predicate);

        public static bool None<TEnumerator, TItem, TState>(this IEnumerable<TEnumerator, TItem> source, in TState state, Func<TItem, int, TState, bool> predicate) where TEnumerator : IEnumerator<TItem> =>
            !source.Any(state, predicate);

        public static bool Contains<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, in TItem value, IEqualityComparer<TItem> comparer = null) where TEnumerator : IEnumerator<TItem>
        {
            comparer = comparer ?? EqualityComparer<TItem>.Default;
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext()) if (comparer.Equals(value, enumerator.Current)) return true;
                return false;
            }
        }

        public static bool Contains<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, in TItem value, Func<TItem, TItem, bool> equals) where TEnumerator : IEnumerator<TItem>
        {
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext()) if (equals(value, enumerator.Current)) return true;
                return false;
            }
        }

        public static int Count<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source) where TEnumerator : IEnumerator<TItem> =>
            source.Count(_ => true);

        public static int Count<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, Func<TItem, bool> predicate) where TEnumerator : IEnumerator<TItem> =>
            source.Count(predicate, (current, _, state) => state(current));

        public static int Count<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, Func<TItem, int, bool> predicate) where TEnumerator : IEnumerator<TItem> =>
            source.Count(predicate, (current, index, state) => state(current, index));

        public static int Count<TEnumerator, TItem, TState>(this IEnumerable<TEnumerator, TItem> source, in TState state, Func<TItem, int, TState, bool> predicate) where TEnumerator : IEnumerator<TItem>
        {
            using (var enumerator = source.GetEnumerator())
            {
                var index = 0;
                var count = 0;
                while (enumerator.MoveNext()) if (predicate(enumerator.Current, index++, state)) count++;
                return count;
            }
        }

        public static bool Same<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, Func<TItem, TItem, bool> equals) where TEnumerator : IEnumerator<TItem>
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    var previous = enumerator.Current;
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        if (equals(previous, current)) previous = current;
                        else return false;
                    }
                }

                return true;
            }
        }

        public static bool Same<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, IEqualityComparer<TItem> comparer) where TEnumerator : IEnumerator<TItem>
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    var previous = enumerator.Current;
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        if (comparer.Equals(previous, current)) previous = current;
                        else return false;
                    }
                }

                return true;
            }
        }

        public static bool SequenceEqual<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> first, IEnumerable<TEnumerator, TItem> second, Func<TItem, TItem, bool> equals) where TEnumerator : IEnumerator<TItem>
        {
            using (var enumerator1 = first.GetEnumerator())
            using (var enumerator2 = second.GetEnumerator())
            {
                while (enumerator1.MoveNext())
                {
                    if (enumerator2.MoveNext() && equals(enumerator1.Current, enumerator2.Current)) continue;
                    return false;
                }
                if (enumerator2.MoveNext()) return false;
                return true;
            }
        }

        public static bool SequenceEqual<TEnumerator, TItem1, TItem2>(this IEnumerable<TEnumerator, TItem1> first, IEnumerable<TItem2> second, Func<TItem1, TItem2, bool> equals) where TEnumerator : IEnumerator<TItem1>
        {
            using (var enumerator1 = first.GetEnumerator())
            using (var enumerator2 = second.GetEnumerator())
            {
                while (enumerator1.MoveNext())
                {
                    if (enumerator2.MoveNext() && equals(enumerator1.Current, enumerator2.Current)) continue;
                    return false;
                }
                if (enumerator2.MoveNext()) return false;
                return true;
            }
        }

        public static bool SequenceEqual<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> first, IEnumerable<TEnumerator, TItem> second, IEqualityComparer<TItem> comparer = null) where TEnumerator : IEnumerator<TItem>
        {
            using (var enumerator1 = first.GetEnumerator())
            using (var enumerator2 = second.GetEnumerator())
            {
                comparer = comparer ?? EqualityComparer<TItem>.Default;
                while (enumerator1.MoveNext())
                {
                    if (enumerator2.MoveNext() && comparer.Equals(enumerator1.Current, enumerator2.Current)) continue;
                    return false;
                }
                if (enumerator2.MoveNext()) return false;
                return true;
            }
        }

        public static bool SequenceEqual<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> first, IEnumerable<TItem> second, IEqualityComparer<TItem> comparer = null) where TEnumerator : IEnumerator<TItem>
        {
            using (var enumerator1 = first.GetEnumerator())
            using (var enumerator2 = second.GetEnumerator())
            {
                comparer = comparer ?? EqualityComparer<TItem>.Default;
                while (enumerator1.MoveNext())
                {
                    if (enumerator2.MoveNext() && comparer.Equals(enumerator1.Current, enumerator2.Current)) continue;
                    return false;
                }
                if (enumerator2.MoveNext()) return false;
                return true;
            }
        }

        public static bool TryFirst<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, out TItem item) where TEnumerator : IEnumerator<TItem> =>
            source.TryFirst(_ => true, out item);

        public static bool TryFirst<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, Func<TItem, bool> predicate, out TItem item) where TEnumerator : IEnumerator<TItem> =>
            source.TryFirst(predicate, (current, _, state) => state(current), out item);

        public static bool TryFirst<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, Func<TItem, int, bool> predicate, out TItem item) where TEnumerator : IEnumerator<TItem> =>
            source.TryFirst(predicate, (current, index, state) => state(current, index), out item);

        public static bool TryFirst<TEnumerator, TItem, TState>(this IEnumerable<TEnumerator, TItem> source, in TState state, Func<TItem, int, TState, bool> predicate, out TItem item) where TEnumerator : IEnumerator<TItem>
        {
            using (var enumerator = source.GetEnumerator())
            {
                var index = 0;
                while (enumerator.MoveNext())
                {
                    item = enumerator.Current;
                    if (predicate(item, index++, state)) return true;
                }

                item = default;
                return false;
            }
        }

        public static bool TryLast<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, out TItem item) where TEnumerator : IEnumerator<TItem> =>
            source.TryLast(_ => true, out item);

        public static bool TryLast<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, Func<TItem, bool> predicate, out TItem item) where TEnumerator : IEnumerator<TItem> =>
            source.TryLast(predicate, (current, _, state) => state(current), out item);

        public static bool TryLast<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, Func<TItem, int, bool> predicate, out TItem item) where TEnumerator : IEnumerator<TItem> =>
            source.TryLast(predicate, (current, index, state) => state(current, index), out item);

        public static bool TryLast<TEnumerator, TItem, TState>(this IEnumerable<TEnumerator, TItem> source, in TState state, Func<TItem, int, TState, bool> predicate, out TItem item) where TEnumerator : IEnumerator<TItem>
        {
            using (var enumerator = source.GetEnumerator())
            {
                var has = false;
                var index = 0;
                item = default;

                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (predicate(current, index++, state))
                    {
                        item = current;
                        has = true;
                    }
                }

                return has;
            }
        }

        public static bool TryAt<TEnumerator, TItem>(this IEnumerable<TEnumerator, TItem> source, int index, out TItem item) where TEnumerator : IEnumerator<TItem>
        {
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (index-- <= 0)
                    {
                        item = enumerator.Current;
                        return true;
                    }
                }

                item = default;
                return false;
            }
        }
    }
}