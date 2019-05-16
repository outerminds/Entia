using System.Runtime.CompilerServices;
using Entia.Components;

namespace Entia.Modules.Component
{
    public static class ComponentsExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAll(this States state, States other) => (state & other) == other;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAny(this States state, States other) => (state & other) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Set(this ref Transient.Resolutions resolution, Transient.Resolutions value)
        {
            if (value > resolution)
            {
                resolution = value;
                return true;
            }

            return false;
        }
    }
}