using System.Collections.Generic;

namespace Entia.Core
{
    public static class Extensions
    {
        public static bool Not(this bool value) => !value;

        public static bool Change<T>(ref this T source, in T target) where T : struct
        {
            var changed = !EqualityComparer<T>.Default.Equals(source, target);
            source = target;
            return changed;
        }
    }
}
