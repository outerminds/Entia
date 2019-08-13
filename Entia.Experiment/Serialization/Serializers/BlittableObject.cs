using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Entia.Core;

namespace Entia.Experiment
{
    public sealed class BlittableObject : ISerializer
    {
        readonly Type Type;
        readonly int Size;

        public BlittableObject(Type type, int size) { Type = type; Size = size; }

        public bool Serialize(object instance, in SerializeContext context)
        {
            var handle = GCHandle.Alloc(instance, GCHandleType.Pinned);
            try
            {
                var pointer = handle.AddrOfPinnedObject();
                context.Writer.Write(pointer, Size);
                return true;
            }
            finally { handle.Free(); }
        }

        public bool Instantiate(out object instance, in DeserializeContext context)
        {
            instance = FormatterServices.GetUninitializedObject(Type);
            var handle = GCHandle.Alloc(instance, GCHandleType.Pinned);
            try
            {
                var pointer = handle.AddrOfPinnedObject();
                return context.Reader.Read(pointer, Size);
            }
            finally { handle.Free(); }
        }

        public bool Initialize(ref object instance, in DeserializeContext context) => true;
    }

    public sealed class BlittableObject<T> : Serializer<T> where T : unmanaged
    {
        public override bool Serialize(in T instance, in SerializeContext context)
        {
            context.Writer.Write(instance);
            return true;
        }

        public override bool Instantiate(out T instance, in DeserializeContext context) => context.Reader.Read(out instance);
        public override bool Initialize(ref T instance, in DeserializeContext context) => true;
    }
}