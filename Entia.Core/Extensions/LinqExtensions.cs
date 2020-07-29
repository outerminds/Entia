using Entia.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Core
{
    public static partial class LinqExtensions
    {
        static class Cache<T>
        {
            public static Func<T, T, bool> Equal = EqualityComparer<T>.Default.Equals;
            public static Comparison<T> Compare = Comparer<T>.Default.Compare;
        }

        public static bool All<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
        {
            var index = 0;
            foreach (var item in source) if (!predicate(item, index++)) return false;
            return true;
        }

        public static bool Any<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
        {
            var index = 0;
            foreach (var item in source) if (predicate(item, index++)) return true;
            return false;
        }

        public static (Option<T> head, T[] tail) Chop<T>(this IEnumerable<T> source)
        {
            var tail = new Queue<T>(source);
            var head = tail.Count > 0 ? tail.Dequeue() : Option.None().AsOption<T>();
            return (head, tail.ToArray());
        }

        public static (T[] head, Option<T> tail) ChopLast<T>(this IEnumerable<T> source)
        {
            var head = new List<T>(source);
            var tail = head.Pop();
            return (head.ToArray(), tail);
        }

        public static int MaxIndex<T>(this IEnumerable<T> source, IComparer<T> comparer = null) =>
            source.MaxIndex(comparer == null ? Cache<T>.Compare : comparer.Compare);

        public static int MaxIndex<T>(this IEnumerable<T> source, Comparison<T> compare)
        {
            var enumerator = source.GetEnumerator();
            if (enumerator.MoveNext())
            {
                var maximum = (value: enumerator.Current, index: 0);
                var index = 0;
                while (enumerator.MoveNext())
                {
                    index++;
                    var current = enumerator.Current;
                    if (compare(current, maximum.value) > 0) maximum = (current, index);
                }
                return index;
            }
            return -1;
        }

        public static int MinIndex<T>(this IEnumerable<T> source, IComparer<T> comparer = null) =>
            source.MinIndex(comparer == null ? Cache<T>.Compare : comparer.Compare);

        public static int MinIndex<T>(this IEnumerable<T> source, Comparison<T> compare)
        {
            var enumerator = source.GetEnumerator();
            if (enumerator.MoveNext())
            {
                var minimum = (value: enumerator.Current, index: 0);
                var index = 0;
                while (enumerator.MoveNext())
                {
                    index++;
                    var current = enumerator.Current;
                    if (compare(current, minimum.value) < 0) minimum = (current, index);
                }
                return index;
            }
            return -1;
        }

        public static IEnumerable<T> Separate<T>(this IEnumerable<T> source, T separator) => source.Separate(() => separator);

        public static IEnumerable<T> Separate<T>(this IEnumerable<T> source, Func<T> provider)
        {
            var first = true;
            var previous = default(T);
            foreach (var item in source)
            {
                if (!first)
                {
                    yield return previous;
                    yield return provider();
                }

                first = false;
                previous = item;
            }

            if (!first) yield return previous;
        }

        public static IEnumerable<T> Repeat<T>(this IEnumerable<T> source, int count)
        {
            for (var i = 0; i < count; i++)
                foreach (var item in source) yield return item;
        }

        public static IEnumerable<T> SkipFirst<T>(this IEnumerable<T> source) => source.Skip(1);

        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
        {
            var first = true;
            var previous = default(T);
            foreach (var item in source)
            {
                if (!first) yield return previous;
                first = false;
                previous = item;
            }
        }

        public static IEnumerable<T> SkipAt<T>(this IEnumerable<T> source, int index) => source.Where((_, current) => current != index);

        public static bool TryFirst<T>(this IEnumerable<T> source, out T item)
        {
            foreach (var current in source)
            {
                item = current;
                return true;
            }

            item = default;
            return false;
        }

        public static bool TryFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate, out T item)
        {
            foreach (var current in source)
            {
                if (predicate(current))
                {
                    item = current;
                    return true;
                }
            }

            item = default;
            return false;
        }

        public static bool TryFirst<T>(this IEnumerable<T> source, Func<T, int, bool> predicate, out T item)
        {
            var index = 0;
            foreach (var current in source)
            {
                if (predicate(current, index++))
                {
                    item = current;
                    return true;
                }
            }

            item = default;
            return false;
        }

        public static bool TryLast<T>(this IEnumerable<T> source, out T item)
        {
            var has = false;
            item = default;

            foreach (var current in source)
            {
                item = current;
                has = true;
            }

            return has;
        }

        public static bool TryLast<T>(this IEnumerable<T> source, Func<T, bool> predicate, out T item)
        {
            var has = false;
            item = default;

            foreach (var current in source)
            {
                if (predicate(current))
                {
                    item = current;
                    has = true;
                }
            }

            return has;
        }

        public static bool TryAt<T>(this IEnumerable<T> source, int index, out T item)
        {
            foreach (var current in source)
            {
                if (index-- <= 0)
                {
                    item = current;
                    return true;
                }
            }

            item = default;
            return false;
        }

        public static (T first, T second)? Two<T>(this IEnumerable<T> source)
        {
            var index = 0;
            var pair = default((T, T));

            foreach (var item in source)
            {
                switch (index++)
                {
                    case 0: pair.Item1 = item; break;
                    case 1: pair.Item2 = item; return pair;
                }
            }

            return null;
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] values)
        {
            foreach (var item in source) yield return item;
            foreach (var value in values) yield return value;
        }

        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector)
        {
            var set = new HashSet<TKey>();
            foreach (var item in source) if (set.Add(selector(item))) yield return item;
        }

        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> source, params T[] values)
        {
            foreach (var value in values) yield return value;
            foreach (var item in source) yield return item;
        }

        public static void Iterate<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            var index = 0;
            foreach (var item in source) action(item, index++);
        }

        public static void Iterate<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source) action(item);
        }

        public static void Iterate<T>(this IEnumerable<T> source)
        {
            foreach (var _ in source) { }
        }

        public static bool None<T>(this IEnumerable<T> source) => !source.Any();

        public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate) => !source.Any(predicate);

        public static IEnumerable<T[]> Combinations<T>(this IEnumerable<T> source, int size)
        {
            if (size <= 0) yield break;
            if (size == 1) foreach (var item in source) yield return new T[] { item };
            else
            {
                var items = source.ToArray();
                if (items.Length < size) yield break;
                if (items.Length == size) yield return items;
                else
                {
                    for (var i = 0; i < items.Length; i++)
                    {
                        foreach (var combination in items.Skip(i + 1).Combinations(size - 1))
                            yield return combination.Prepend(items[i]);
                    }
                }
            }
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, params IEnumerable<T>[] others)
        {
            foreach (var item in source) yield return item;
            foreach (var other in others) foreach (var item in other) yield return item;
        }

        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, params T[] values) => source.Except(values.AsEnumerable());

        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, params object[] values) where T : class =>
            source.Except(values.AsEnumerable());

        public static IEnumerable<T> OfType<T>(this IEnumerable<T> source, Type type, bool hierarchy = false, bool definition = false) =>
            source.Where(item => TypeUtility.Is(item, type, hierarchy, definition));

        public static bool Same<T>(this IEnumerable<T> source, Func<T, T, bool> equals)
        {
            using var enumerator = source.GetEnumerator();
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

        public static bool Same<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null) =>
            source.Same(comparer == null ? Cache<T>.Equal : comparer.Equals);

        public static IEnumerable<TResult> Select<TSource, TResult, TState>(this IEnumerable<TSource> source, TState state, Func<TSource, TState, TResult> selector)
        {
            foreach (var item in source) yield return selector(item, state);
        }

        public static IEnumerable<TResult> SelectMany<TSource, TResult, TState>(this IEnumerable<TSource> source, TState state, Func<TSource, TState, IEnumerable<TResult>> selector)
        {
            foreach (var item in source) foreach (var sub in selector(item, state)) yield return sub;
        }

        public static IEnumerable<T> Some<T>(this IEnumerable<T> source) where T : class => source.Where(value => value != null);
        public static IEnumerable<T> Some<T>(this IEnumerable<T?> source) where T : struct => source.Where(value => value.HasValue).Select(value => value.Value);
        public static IEnumerable<TSource> SomeBy<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) where TResult : class =>
            source.Where(value => selector(value) != null);
        public static IEnumerable<TSource> SomeBy<TSource, TResult>(this IEnumerable<TSource?> source, Func<TSource, TResult> selector) where TSource : struct where TResult : class =>
            source.Where(value => value.HasValue && selector(value.Value) != null).Select(value => value.Value);

        public static (T[] @true, T[] @false) Split<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var @true = new List<T>();
            var @false = new List<T>();
            foreach (var item in source)
            {
                if (predicate(item)) @true.Add(item);
                else @false.Add(item);
            }

            return (@true.ToArray(), @false.ToArray());
        }

        public static HashSet<T> ToSet<T>(this IEnumerable<T> source) => new HashSet<T>(source);

        public static IEnumerable<TResult> TrySelect<TSource, TResult>(this IEnumerable<TSource> source, TryFunc<TSource, TResult> selector)
        {
            foreach (var item in source) if (selector(item, out var value)) yield return value;
        }

        public static IEnumerable<TResult> TrySelect<TSource, TResult, TState>(this IEnumerable<TSource> source, TState state, TryFunc<TSource, TState, TResult> selector)
        {
            foreach (var item in source) if (selector(item, state, out var value)) yield return value;
        }

        public static IEnumerable<T[]> Window<T>(this IEnumerable<T> source, int size)
        {
            if (size <= 0) yield break;

            var items = source.ToArray();
            for (var i = 0; i <= items.Length - size; i++)
            {
                var window = new T[size];
                Array.Copy(items, i, window, 0, size);
                yield return window;
            }
        }

        public static IEnumerable<T> Where<T, TState>(this IEnumerable<T> source, TState state, Func<T, TState, bool> predicate)
        {
            foreach (var item in source) if (predicate(item, state)) yield return item;
        }
    }
}
