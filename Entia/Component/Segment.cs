using Entia.Components;
using Entia.Core;
using Entia.Core.Documentation;
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
        public readonly Metadata[] Types;
        public readonly Metadata[] Components;
        public readonly Metadata[] Tags;
        /// <summary>
        /// The entities.
        /// </summary>
        public (Entity[] items, int count) Entities;

        int _minimum;
        int _maximum;
        Array[] _stores;

        /// <summary>
        /// Initializes a new instance of the <see cref="Segment"/> class.
        /// </summary>
        public Segment(int index, BitMask mask, int capacity = 8)
        {
            Index = index;
            Mask = mask;

            Types = ComponentUtility.ToMetadata(mask);
            Components = Types.Where(type => type.Kind == Metadata.Kinds.Data).ToArray();
            Tags = Types.Where(type => type.Kind == Metadata.Kinds.Tag).ToArray();
            Entities = (new Entity[capacity], 0);

            _minimum = Components.Select(type => type.Index).FirstOrDefault();
            _maximum = Components.Select(type => type.Index + 1).LastOrDefault();
            _stores = new Array[_maximum - _minimum];
            foreach (var type in Components) _stores[GetStoreIndex(type)] = Array.CreateInstance(type.Type, capacity);
        }

        /// <summary>
        /// Tries the get the component store of provided component <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The component type.</param>
        /// <param name="store">The component store.</param>
        /// <returns>Returns <c>true</c> if a component store was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool TryStore(in Metadata type, out Array store)
        {
            var index = GetStoreIndex(type);
            if (index >= 0 && index < _stores.Length && _stores[index] is Array array)
            {
                store = array;
                return true;
            }

            store = default;
            return false;
        }
        /// <summary>
        /// Gets the component store of provided component <paramref name="type"/>.
        /// If the store doesn't exist, an <see cref="IndexOutOfRangeException"/> may be thrown or a <c>null</c> will be returned.
        /// Use <see cref="TryStore(in Metadata, out Array)"/> if you are unsure if the store exists.
        /// </summary>
        /// <param name="type">The component type.</param>
        /// <returns>The component store.</returns>
        [ThreadSafe]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Array Store(in Metadata type) => _stores[GetStoreIndex(type)];
        /// <summary>
        /// Ensures that all component stores are at least of the same size as the <see cref="Entities"/> array.
        /// If a component store not large enough, it is resized.
        /// </summary>
        /// <returns>Returns <c>true</c> if a component store was resized; otherwise, <c>false</c>.</returns>
        public bool Ensure()
        {
            var resized = false;
            for (int i = 0; i < Components.Length; i++)
            {
                ref readonly var metadata = ref Components[i];
                var index = GetStoreIndex(metadata);
                ref var store = ref _stores[index];
                resized |= ArrayUtility.Ensure(ref store, metadata.Type, Entities.count);
            }
            return resized;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ThreadSafe]
        int GetStoreIndex(in Metadata metadata) => metadata.Index - _minimum;
    }
}
