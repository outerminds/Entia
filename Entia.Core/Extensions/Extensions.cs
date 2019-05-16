using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Entia.Core
{
    public static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Not(this bool value) => !value;

        public static ref T Swap<T>(ref this T source, ref T target) where T : struct
        {
            var temporary = source;
            source = target;
            target = temporary;
            return ref target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Change(ref this bool source, bool target)
        {
            var changed = source != target;
            source = target;
            return changed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Change(ref this byte source, byte target)
        {
            var changed = source != target;
            source = target;
            return changed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Change(ref this sbyte source, sbyte target)
        {
            var changed = source != target;
            source = target;
            return changed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Change(ref this short source, short target)
        {
            var changed = source != target;
            source = target;
            return changed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Change(ref this ushort source, ushort target)
        {
            var changed = source != target;
            source = target;
            return changed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Change(ref this int source, int target)
        {
            var changed = source != target;
            source = target;
            return changed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Change(ref this uint source, uint target)
        {
            var changed = source != target;
            source = target;
            return changed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Change(ref this ulong source, ulong target)
        {
            var changed = source != target;
            source = target;
            return changed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Change(ref this long source, long target)
        {
            var changed = source != target;
            source = target;
            return changed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Change(ref this float source, float target)
        {
            var changed = source != target;
            source = target;
            return changed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Change(ref this double source, double target)
        {
            var changed = source != target;
            source = target;
            return changed;
        }

        public static bool Change<T>(ref this T source, in T target) where T : struct
        {
            var changed = !EqualityComparer<T>.Default.Equals(source, target);
            source = target;
            return changed;
        }
    }
}
