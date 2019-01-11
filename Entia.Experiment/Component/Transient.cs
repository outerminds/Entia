using Entia.Core;
using System;

namespace Entia.Modules.Component
{
    public sealed class Transient
    {
        public const int ChunkSize = 8;

        public ((Entity entity, BitMask mask)[] items, int count) Entities = (new (Entity, BitMask)[16], 0);
        public Array[][] Chunks = new Array[2][];

        public int Reserve(Entity entity, BitMask mask)
        {
            var index = Entities.count++;
            Entities.Ensure();

            ref var pair = ref Entities.items[index];
            if (pair.mask == null) pair = (entity, new BitMask { mask });
            else
            {
                pair.entity = entity;
                pair.mask.Clear();
                pair.mask.Add(mask);
            }

            return index;
        }

        public bool TryStore<T>(int index, out T[] store) where T : struct, IComponent
        {
            var component = ComponentUtility.Cache<T>.Data.Index;
            if (TryChunk(index / ChunkSize, out var chunk) && component < chunk.Length)
            {
                store = chunk[component] as T[];
                return store != null;
            }

            store = default;
            return false;
        }

        public T[] Store<T>(int index, out int adjusted) where T : struct, IComponent => Store(index, ComponentUtility.Cache<T>.Data, out adjusted) as T[];

        public Array Store(int index, in Metadata metadata, out int adjusted)
        {
            var chunk = Chunk(index, metadata.Index + 1);
            adjusted = index % ChunkSize;

            if (chunk[metadata.Index] is Array store) return store;
            chunk[metadata.Index] = store = Array.CreateInstance(metadata.Type, ChunkSize);
            return store;
        }

        bool TryChunk(int index, out Array[] chunk)
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

        Array[] Chunk(int index, int count)
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
