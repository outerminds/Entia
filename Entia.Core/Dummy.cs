namespace Entia.Core
{
    public static class Dummy<T>
    {
        public static class Read
        {
            public static readonly T Value;
        }

        public static T Value;
    }
}