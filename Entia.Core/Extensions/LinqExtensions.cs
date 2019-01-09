using Entia.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Core
{
    public static class LinqExtensions
    {
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

        public static IEnumerable<T> Separate<T>(this IEnumerable<T> source, T separator)
        {
            var first = true;
            var previous = default(T);
            foreach (var item in source)
            {
                if (!first)
                {
                    yield return previous;
                    yield return separator;
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

        public static bool TryFirst<T>(this IEnumerable<T> source, out T first)
        {
            foreach (var item in source)
            {
                first = item;
                return true;
            }

            first = default;
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

        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> by)
        {
            var set = new HashSet<TKey>();
            foreach (var item in source) if (set.Add(by(item))) yield return item;
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
            foreach (var item in source) { }
        }

        public static bool None<T>(this IEnumerable<T> source) => !source.Any();

        public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate) => !source.Any(predicate);

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, params IEnumerable<T>[] others)
        {
            foreach (var item in source) yield return item;
            foreach (var other in others) foreach (var item in other) yield return item;
        }

        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, params T[] values) => source.Except(values.AsEnumerable());

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
                            yield return combination.Prepend(items[i]).ToArray();
                    }
                }
            }
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, int seed)
        {
            var random = new Random(seed);
            return source.OrderBy(_ => random.Next(-100, 100));
        }

        public static IEnumerable<T> OfType<T>(this IEnumerable<T> source, Type type, bool hierarchy = false, bool definition = false) =>
            source.Where(item => item.Is(type, hierarchy, definition));

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

        public static IEnumerable<T> Some<T>(this IEnumerable<T> source) where T : class => source.Where(value => value != null);
        public static IEnumerable<T> Some<T>(this IEnumerable<T?> source) where T : struct => source.Where(value => value.HasValue).Select(value => value.Value);
        public static IEnumerable<T1> SomeBy<T1, T2>(this IEnumerable<T1> source, Func<T1, T2> selector) where T2 : class =>
            source.Where(value => selector(value) != null);
        public static IEnumerable<T1> SomeBy<T1, T2>(this IEnumerable<T1?> source, Func<T1, T2> selector) where T1 : struct where T2 : class =>
            source.Where(value => value.HasValue && selector(value.Value) != null).Select(value => value.Value);
    }
}
