using Entia.Core;

namespace Entia.Json.Converters
{
    public sealed class Item<T>
    {
        public readonly int Index;
        public readonly Item.Convert<T> Convert;
        public readonly Item.Initialize<T> Initialize;

        public Item(int index, Item.Convert<T> convert, Item.Initialize<T> initialize)
        {
            Index = index;
            Convert = convert;
            Initialize = initialize;
        }
    }

    public static class Item
    {
        public delegate Node Convert<T>(in T instance, in ConvertToContext context);
        public delegate void Initialize<T>(ref T instance, in ConvertFromContext context);
        public delegate ref readonly TValue Get<T, TValue>(in T instance);
        public delegate TValue Getter<T, TValue>(in T instance);
        public delegate void Setter<T, TValue>(ref T instance, in TValue value);
        public delegate Node To<T>(in T instance, in ConvertToContext context);
        public delegate T From<T>(Node node, in ConvertFromContext context);

        static class Cache<T>
        {
            public static readonly To<T> To = (in T value, in ConvertToContext context) => context.Convert(value);
            public static readonly From<T> From = (Node node, in ConvertFromContext context) => context.Convert<T>(node);
        }

        public static Item<T> Field<T, TValue>(int index, Get<T, TValue> get, To<TValue> to = null, From<TValue> from = null)
        {
            to ??= Cache<TValue>.To;
            from ??= Cache<TValue>.From;
            return new Item<T>(index,
                (in T instance, in ConvertToContext context) => to(get(instance), context),
                (ref T instance, in ConvertFromContext context) => UnsafeUtility.Set(get(instance), from(context.Node, context)));
        }

        public static Item<T> Property<T, TValue>(int index, Getter<T, TValue> get, Setter<T, TValue> set, To<TValue> to = null, From<TValue> from = null)
        {
            to ??= Cache<TValue>.To;
            from ??= Cache<TValue>.From;
            return new Item<T>(index,
                (in T instance, in ConvertToContext context) => to(get(instance), context),
                (ref T instance, in ConvertFromContext context) => set(ref instance, from(context.Node, context)));
        }
    }
}