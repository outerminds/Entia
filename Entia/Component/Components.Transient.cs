using Entia.Core;
using Entia.Core.Documentation;
using Entia.Modules.Component;
using System;
using System.Runtime.CompilerServices;

namespace Entia.Modules
{
    public sealed partial class Components
    {
        enum Resolutions : byte { None = 0, Move = 1, Initialize = 2, Dispose = 3 }
        struct Slot
        {
            public Entity Entity;
            public BitMask Mask;
            public BitMask Lock;
            public Resolutions Resolution;
        }

        const int ChunkSize = 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool SetResolution(ref Resolutions resolution, Resolutions value)
        {
            if (value > resolution)
            {
                resolution = value;
                return true;
            }

            return false;
        }

        int ReserveTransient(Entity entity, Resolutions resolution, BitMask mask = null)
        {
            var index = _slots.count++;
            _slots.Ensure();

            ref var slot = ref _slots.items[index];
            if (slot.Mask == null || slot.Lock == null)
                slot = new Slot { Entity = entity, Mask = new BitMask(), Lock = new BitMask(), Resolution = resolution };
            else
            {
                slot.Entity = entity;
                slot.Resolution = resolution;
                slot.Mask.Clear();
                slot.Lock.Clear();
            }

            if (mask != null) slot.Mask.Add(mask);
            return index;
        }

        [ThreadSafe]
        bool TryGetTransientStore(int index, in Metadata metadata, out Array store, out int adjusted)
        {
            if (TryGetTransientChunk(index, out var chunk) && metadata.Index < chunk.Length && chunk[metadata.Index] is Array array)
            {
                store = array;
                adjusted = index % ChunkSize;
                return true;
            }

            store = default;
            adjusted = default;
            return false;
        }

        Array GetTransientStore(int index, in Metadata metadata, out int adjusted)
        {
            var chunk = GetTransientChunk(index, metadata.Index + 1);
            adjusted = index % ChunkSize;

            if (chunk[metadata.Index] is Array store) return store;
            return chunk[metadata.Index] = Array.CreateInstance(metadata.Type, ChunkSize);
        }

        [ThreadSafe]
        bool TryGetTransientChunk(int index, out Array[] chunk)
        {
            index /= ChunkSize;
            if (index < _chunks.Length)
            {
                chunk = _chunks[index];
                return chunk != null;
            }

            chunk = default;
            return false;
        }

        Array[] GetTransientChunk(int index, int count)
        {
            index /= ChunkSize;
            ArrayUtility.Ensure(ref _chunks, index + 1);

            ref var chunk = ref _chunks[index];
            if (chunk == null) return chunk = new Array[count];
            ArrayUtility.Ensure(ref chunk, count);
            return chunk;
        }
    }
}