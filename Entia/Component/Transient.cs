using Entia.Core;
using Entia.Core.Documentation;
using System;
using System.Threading;

namespace Entia.Modules.Component
{
    public sealed class Transient
    {
        public enum Resolutions : byte { None = 0, Move = 1, Initialize = 2, Dispose = 3 }
        public struct Slot
        {
            public Entity Entity;
            public BitMask Mask;
            public Resolutions Resolution;
        }

        public const int ChunkSize = 8;

        public (Slot[] items, int count) Slots = (new Slot[16], 0);
        public Array[][] Chunks = new Array[2][];

        public int Reserve(Entity entity, Resolutions resolution, BitMask mask = null)
        {
            var index = Slots.count++;
            Slots.Ensure();

            ref var slot = ref Slots.items[index];
            if (slot.Mask == null) slot = new Slot { Entity = entity, Mask = new BitMask(), Resolution = resolution };
            else
            {
                slot.Entity = entity;
                slot.Resolution = resolution;
                slot.Mask.Clear();
            }

            if (mask != null) slot.Mask.Add(mask);
            return index;
        }

        [ThreadSafe]
        public bool TryStore<T>(int index, out T[] store, out int adjusted) where T : struct, IComponent
        {
            if (TryStore(index, ComponentUtility.Cache<T>.Data, out var array, out adjusted) && array is T[] casted)
            {
                store = casted;
                return true;
            }

            store = default;
            return false;
        }

        [ThreadSafe]
        public bool TryStore(int index, in Metadata metadata, out Array store, out int adjusted)
        {
            if (TryGetChunk(index, out var chunk) && metadata.Index < chunk.Length && chunk[metadata.Index] is Array array)
            {
                store = array;
                adjusted = index % ChunkSize;
                return true;
            }

            store = default;
            adjusted = default;
            return false;
        }

        public T[] Store<T>(int index, out int adjusted) where T : struct, IComponent
        {
            ref readonly var metadata = ref ComponentUtility.Cache<T>.Data;
            var chunk = GetChunk(index, metadata.Index + 1);
            adjusted = index % ChunkSize;

            if (chunk[metadata.Index] is T[] store) return store;
            chunk[metadata.Index] = store = new T[ChunkSize];
            return store;
        }

        public Array Store(int index, in Metadata metadata, out int adjusted)
        {
            var chunk = GetChunk(index, metadata.Index + 1);
            adjusted = index % ChunkSize;

            if (chunk[metadata.Index] is Array store) return store;
            return chunk[metadata.Index] = Array.CreateInstance(metadata.Type, ChunkSize);
        }

        [ThreadSafe]
        bool TryGetChunk(int index, out Array[] chunk)
        {
            index /= ChunkSize;
            if (index < Chunks.Length)
            {
                chunk = Chunks[index];
                return chunk != null;
            }

            chunk = default;
            return false;
        }

        Array[] GetChunk(int index, int count)
        {
            index /= ChunkSize;
            ArrayUtility.Ensure(ref Chunks, index + 1);

            ref var chunk = ref Chunks[index];
            if (chunk == null) return chunk = new Array[count];
            ArrayUtility.Ensure(ref chunk, count);
            return chunk;
        }
    }
}
