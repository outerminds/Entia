using Entia.Core;

namespace Entia.Json.Converters
{
    /// <summary>
    /// Data structure that represents an item of an array converter.
    /// </summary>
    /// <remarks>
    /// See <see cref="Converter.Array{T}"/>.
    /// </remarks>
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

    /// <summary>
    /// Module that exposes constructors for <see cref="Item{T}"/>.
    /// </summary>
    /// <remarks>
    /// See <see cref="Converter.Array{T}"/>.
    /// </remarks>
    public static class Item
    {
        public delegate Node Convert<T>(in T instance, in ToContext context);
        public delegate void Initialize<T>(ref T instance, in FromContext context);
        public delegate ref readonly TValue Get<T, TValue>(in T instance);
        public delegate TValue Getter<T, TValue>(in T instance);
        public delegate void Setter<T, TValue>(ref T instance, in TValue value);

        public static Item<T> Field<T, TValue>(uint index, Get<T, TValue> get, Converter<TValue> converter = null) =>
            new Item<T>(index,
                (in T instance, in ToContext context) => context.Convert(get(instance), converter, converter),
                (ref T instance, in FromContext context) => UnsafeUtility.Set(get(instance), context.Convert<TValue>(context.Node, converter, converter)));

        public static Item<T> Property<T, TValue>(uint index, Getter<T, TValue> get, Setter<T, TValue> set, Converter<TValue> converter = null) =>
            new Item<T>(index,
                (in T instance, in ToContext context) => context.Convert(get(instance), converter, converter),
                (ref T instance, in FromContext context) => set(ref instance, context.Convert<TValue>(context.Node, converter, converter)));
    }
}