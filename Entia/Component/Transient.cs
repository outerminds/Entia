using Entia.Core;
using System;

namespace Entia.Modules.Component
{
    sealed class Transient
    {
        public enum Resolutions : byte { Move, Add, Remove }
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

        public bool TryGetStore<T>(int index, out T[] store) where T : struct, IComponent
        {
            var component = ComponentUtility.Cache<T>.Data.Index;
            if (TryGetChunk(index / ChunkSize, out var chunk) && component < chunk.Length)
            {
                store = chunk[component] as T[];
                return store != null;
            }

            store = default;
            return false;
        }

        public T[] GetStore<T>(int index, out int adjusted) where T : struct, IComponent => GetStore(index, ComponentUtility.Cache<T>.Data, out adjusted) as T[];

        public Array GetStore(int index, in Metadata metadata, out int adjusted)
        {
            var chunk = GetChunk(index, metadata.Index + 1);
            adjusted = index % ChunkSize;

            if (chunk[metadata.Index] is Array store) return store;
            chunk[metadata.Index] = store = Array.CreateInstance(metadata.Type, ChunkSize);
            return store;
        }

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
            if (chunk == null) return Chunks[index] = new Array[count];

            ArrayUtility.Ensure(ref chunk, count);
            return chunk;
        }
    }
}
