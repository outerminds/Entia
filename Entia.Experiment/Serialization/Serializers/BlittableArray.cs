using System;
using System.Runtime.InteropServices;
using Entia.Core;

namespace Entia.Experiment
{
    public sealed class BlittableArray : Serializer<Array>
    {
        readonly Type Type;
        readonly int Size;

        public BlittableArray(Type type, int size) { Type = type; Size = size; }

        public override bool Serialize(in Array instance, in SerializeContext context)
        {
            context.Writer.Write(instance.Length);
            var handle = GCHandle.Alloc(instance, GCHandleType.Pinned);
            try
            {
                var pointer = handle.AddrOfPinnedObject();
                context.Writer.Write(pointer, Size, instance.Length);
                return true;
            }
            finally { handle.Free(); }
        }

        public override bool Instantiate(out Array instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int count))
            {
                instance = Array.CreateInstance(Type, count);
                var handle = GCHandle.Alloc(instance, GCHandleType.Pinned);
                try
                {
                    var pointer = handle.AddrOfPinnedObject();
                    return context.Reader.Read(pointer, Size, count);
                }
                finally { handle.Free(); }
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref Array instance, in DeserializeContext context) => true;
    }

    public sealed class BlittableArray<T> : Serializer<T[]> where T : unmanaged
    {
        public override bool Serialize(in T[] instance, in SerializeContext context)
        {
            context.Writer.Write(instance.Length);
            context.Writer.Write(instance);
            return true;
        }

        public override bool Instantiate(out T[] instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int count) && context.Reader.Read(out instance, count)) return true;
            instance = default;
            return false;
        }

        public override bool Initialize(ref T[] instance, in DeserializeContext context) => true;
    }
}