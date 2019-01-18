using Entia.Core;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Entia.Modules.Component
{
    public sealed class Segment
    {
        public static readonly Segment Empty = new Segment(-1, new BitMask(), 0);

        public readonly int Index;
        public readonly BitMask Mask;
        public readonly (Metadata[] data, int minimum, int maximum) Types;
        public Array[] Stores;
        public (Entity[] items, int count) Entities;

        public Segment(int index, BitMask mask, int capacity = 8)
        {
            Index = index;
            Mask = mask;

            var metadata = Mask
                .Select(bit => ComponentUtility.TryGetMetadata(bit, out var data) ? data : default)
                .Where(data => data.IsValid)
                .ToArray();
            Types = (metadata, metadata.Select(data => data.Index).FirstOrDefault(), metadata.Select(data => data.Index + 1).LastOrDefault());
            Entities = (new Entity[capacity], 0);
            Stores = new Array[Types.maximum - Types.minimum];
            foreach (var datum in Types.data) Stores[GetStoreIndex(datum.Index)] = Array.CreateInstance(datum.Type, capacity);
        }

        public bool Has<T>() where T : struct, IComponent => Has(ComponentUtility.Cache<T>.Data.Index);
        public bool Has(int component) => Mask.Has(component);

        public bool TryGetStore<T>(out T[] store) where T : struct, IComponent
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] GetStore<T>() where T : struct, IComponent => (T[])Stores[GetStoreIndex<T>()];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Array GetStore(int component) => ref Stores[GetStoreIndex(component)];

        public bool TryGetStore(in Metadata metadata, out Array store)
        {
            var index = GetStoreIndex(metadata.Index);
            if (index >= 0 && index < Stores.Length && Stores[index] is Array casted)
            {
                store = casted;
                return true;
            }

            store = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetStoreIndex(int component) => component - Types.minimum;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetStoreIndex<T>() where T : struct, IComponent => GetStoreIndex(ComponentUtility.Cache<T>.Data.Index);
    }
}
