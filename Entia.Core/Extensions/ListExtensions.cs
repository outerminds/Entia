using System;
using System.Collections.Generic;

namespace Entia.Core
{
    public static class ListExtensions
    {
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
