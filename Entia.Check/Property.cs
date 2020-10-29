using System;

namespace Entia.Check
{
    public readonly struct Property<T>
    {
        public readonly string Name;
        public readonly Func<T, bool> Prove;

        public Property(string name, Func<T, bool> prove)
        {
            Name = name;
            Prove = prove;
        }
    }

    public static class Property
    {
        public static Property<T> From<T>(string name, Func<T, bool> prove) => new Property<T>(name, prove);
        public static Property<T> From<T>(Func<T, bool> prove) => From(prove.Method.Name, prove);
    }
}