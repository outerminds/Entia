using System;
using System.Collections.Generic;

namespace Entia.Core
{
    public static class ListExtensions
    {
        public static List<TResult> Map<TSource, TResult>(this List<TSource> list, Func<TSource, TResult> map)
        {
            var results = new List<TResult>(list.Count);
            for (int i = 0; i < list.Count; i++) results[i] = map(list[i]);
            return results;
        }

        public static List<TResult> Map<TSource, TResult, TState>(this List<TSource> list, in TState state, Func<TSource, TState, TResult> map)
        {
            var results = new List<TResult>(list.Count);
            for (int i = 0; i < list.Count; i++) results[i] = map(list[i], state);
            return results;
        }

        public static Option<T> Pop<T>(this List<T> list)
        {
            if (list.Count > 0)
            {
                var index = list.Count - 1;
                var item = list[index];
                list.RemoveAt(index);
                return item;
            }

            return Option.None();
        }
    }
}
