using System.Collections.Generic;

namespace Entia.Core
{
    public static class Extensions
    {
        public static bool Not(this bool value) => !value;

        public static bool Change<T>(ref this T source, in T target) where T : struct =>
            !EqualityComparer<T>.Default.Equals(source, (source = target));
    }
}
