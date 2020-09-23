using Entia.Core;

namespace Entia.Json.Converters
{
    public sealed class Item<T>
    {
        public readonly uint Index;
        public readonly Item.Convert<T> Convert;
        public readonly Item.Initialize<T> Initialize;

        public Item(uint index, Item.Convert<T> convert, Item.Initialize<T> initialize)
        {
            Index = index;
            Convert = convert;
            Initialize = initialize;
        }
    }

    public static class Item
    {
        public delegate Node Convert<T>(in T instance, in ToContext context);
        public delegate void Initialize<T>(ref T instance, in FromContext context);
        public delegate ref readonly TValue Get<T, TValue>(in T instance);
        public delegate TValue Getter<T, TValue>(in T instance);
        public delegate void Setter<T, TValue>(ref T instance, in TValue value);
        public delegate Node To<T>(in T instance, in ToContext context);
        public delegate T From<T>(Node node, in FromContext context);

        public static Item<T> Field<T, TValue>(uint index, Get<T, TValue> get, To<TValue> to = null, From<TValue> from = null, Converter<TValue> converter = null)
        {
            to ??= (in TValue value, in ToContext context) => context.Convert(value, converter, converter);
            from ??= (Node node, in FromContext context) => context.Convert<TValue>(node, converter, converter);
            return new Item<T>(index,
                (in T instance, in ToContext context) => to(get(instance), context),
                (ref T instance, in FromContext context) => UnsafeUtility.Set(get(instance), from(context.Node, context)));
        }

        public static Item<T> Property<T, TValue>(uint index, Getter<T, TValue> get, Setter<T, TValue> set, To<TValue> to = null, From<TValue> from = null, Converter<TValue> converter = null)
        {
            to ??= (in TValue value, in ToContext context) => context.Convert(value, converter, converter);
            from ??= (Node node, in FromContext context) => context.Convert<TValue>(node, converter, converter);
            return new Item<T>(index,
                (in T instance, in ToContext context) => to(get(instance), context),
                (ref T instance, in FromContext context) => set(ref instance, from(context.Node, context)));
        }
    }
}