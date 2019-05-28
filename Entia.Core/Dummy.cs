namespace Entia.Core
{
    public static class Dummy<T>
    {
        public static class Read
        {
            public static readonly T Value;
        }

        public static class Array
        {
            public static readonly T[] Zero = new T[0];
            public static readonly T[] One = new T[1];
        }

        public static T Value;
    }
}