namespace Entia.Core
{
    public static class StringExtensions
    {
        public static bool TryFirst(this string @string, out char value) => @string.TryAt(0, out value);
        public static bool TryLast(this string @string, out char value) => @string.TryAt(@string.Length - 1, out value);

        public static bool TryAt(this string @string, int index, out char value)
        {
            if (index >= 0 && index < @string.Length)
            {
                value = @string[index];
                return true;
            }
            value = default;
            return false;
        }
    }
}