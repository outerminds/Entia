using System;

namespace Entia.Core
{
    public static class MathUtility
    {
        public static int NextPowerOfTwo(int value)
        {
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value + 1;
        }

        public static uint NextPowerOfTwo(uint value)
        {
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value + 1;
        }

        public static int PreviousPowerOfTwo(int value) => NextPowerOfTwo(value - 1) / 2;
        public static uint PreviousPowerOfTwo(uint value) => NextPowerOfTwo(value - 1) / 2;
        public static int CurrentPowerOfTwo(int value) => NextPowerOfTwo(value) / 2;
        public static uint CurrentPowerOfTwo(uint value) => NextPowerOfTwo(value) / 2;

        public static int ClampToInt(uint value) => (int)Math.Min(int.MaxValue, value);
        public static int ClampToInt(ulong value) => (int)Math.Min(int.MaxValue, value);
        public static int ClampToInt(long value) => (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, value));
    }
}
