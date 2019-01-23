using Entia.Core;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Entia.Modules.Component
{
    /// <summary>
    /// Stores the entities and components for a specific component profile represented by the <see cref="Mask"/>.
    /// </summary>
    public sealed class Segment
    {
        /// <summary>
        /// The index of the segment.
        /// </summary>
        public readonly int Index;
        /// <summary>
        /// The mask that represents the component profile of the segment.
        /// </summary>
        public readonly BitMask Mask;
        /// <summary>
        /// The selection of component types that are stored in this segment with the minimum and maximum indices of those types.
        /// </summary>
        public readonly (Metadata[] data, int minimum, int maximum) Types;
        /// <summary>
        /// The component stores.
        /// </summary>
        public Array[] Stores;
        /// <summary>
        /// The entities.
        /// </summary>
        public (Entity[] items, int count) Entities;

        /// <summary>
        /// Initializes a new instance of the <see cref="Segment"/> class.
        /// </summary>
        public Segment(int index, BitMask mask, int capacity = 8)
        {
            Index = index;
            Mask = mask;

            var metadata = ComponentUtility.ToMetadata(mask);
            Types = (metadata, metadata.Select(data => data.Index).FirstOrDefault(), metadata.Select(data => data.Index + 1).LastOrDefault());
            Entities = (new Entity[capacity], 0);
            Stores = new Array[Types.maximum - Types.minimum];
            foreach (var datum in Types.data) Stores[GetStoreIndex(datum.Index)] = Array.CreateInstance(datum.Type, capacity);
        }

        /// <summary>
        /// Tries the get the component store of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="store">The store.</param>
        /// <returns>Returns <c>true</c> if a component store was found; otherwise, <c>false</c>.</returns>
        public bool TryStore<T>(out T[] store) where T : struct, IComponent
        {
            var index = GetStoreIndex<T>();
            if (index >= 0 && index < Stores.Length && Stores[index] is T[] casted)
            {
                store = casted;
                return true;
            }

            store = default;
            return false;
        }

        /// <summary>
        /// Tries the get the component store of provided component type <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The component type index.</param>
        /// <param name="store">The component store.</param>
        /// <returns>Returns <c>true</c> if a component store was found; otherwise, <c>false</c>.</returns>
        public bool TryStore(int index, out Array store)
        {
            var storeIndex = GetStoreIndex(index);
            if (storeIndex >= 0 && storeIndex < Stores.Length && Stores[storeIndex] is Array casted)
            {
                store = casted;
                return true;
            }

            store = default;
            return false;
        }

        /// <summary>
        /// Gets the component store of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>The component store.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] Store<T>() where T : struct, IComponent => (T[])Stores[GetStoreIndex<T>()];
        /// <summary>
        /// Gets the component store of provided component type <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The component type index.</param>
        /// <returns>The component store.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Array Store(int index) => ref Stores[GetStoreIndex(index)];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetStoreIndex(int component) => component - Types.minimum;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetStoreIndex<T>() where T : struct, IComponent => GetStoreIndex(ComponentUtility.Concrete<T>.Data.Index);
    }
}
