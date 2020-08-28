using System;
using System.Runtime.InteropServices;
using Entia.Core;
using Entia.Experimental.Serialization;

namespace Entia.Experimental.Serializers
{
    public sealed class ConcreteArray : Serializer<Array>
    {
        public readonly Type Element;

        readonly TypeData _data;

        public ConcreteArray(Type element) { Element = element; _data = Element; }

        public override bool Serialize(in Array instance, in SerializeContext context)
        {
            context.Writer.Write(instance.Length);
            if (context.Options.Has(Options.Blittable) && _data.Size.TryValue(out var size))
            {
                var handle = GCHandle.Alloc(instance, GCHandleType.Pinned);
                try
                {
                    var pointer = handle.AddrOfPinnedObject();
                    context.Writer.Write(pointer, size, instance.Length);
                    return true;
                }
                finally { handle.Free(); }
            }
            else
            {
                for (int i = 0; i < instance.Length; i++)
                {
                    if (context.Serialize(instance.GetValue(i), Element)) continue;
                    return false;
                }
                return true;
            }
        }

        public override bool Instantiate(out Array instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int count))
            {
                instance = Array.CreateInstance(Element, count);
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref Array instance, in DeserializeContext context)
        {
            if (context.Options.Has(Options.Blittable) && _data.Size.TryValue(out var size))
            {
                var handle = GCHandle.Alloc(instance, GCHandleType.Pinned);
                try
                {
                    var pointer = handle.AddrOfPinnedObject();
                    return context.Reader.Read(pointer, size, instance.Length);
                }
                finally { handle.Free(); }
            }
            else
            {
                for (int i = 0; i < instance.Length; i++)
                {
                    if (context.Deserialize(out var value, Element)) instance.SetValue(value, i);
                    else return false;
                }
                return true;
            }
        }
    }

    public sealed class ConcreteArray<T> : Serializer<T[]>
    {
        public readonly Serializer<T> Element;

        public ConcreteArray() { }
        public ConcreteArray(Serializer<T> element = null) { Element = element; }

        public override bool Serialize(in T[] instance, in SerializeContext context)
        {
            context.Writer.Write(instance.Length);
            for (int i = 0; i < instance.Length; i++)
            {
                if (context.Serialize(instance[i], Element)) continue;
                return false;
            }
            return true;
        }

        public override bool Instantiate(out T[] instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int count))
            {
                instance = new T[count];
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref T[] instance, in DeserializeContext context)
        {
            for (int i = 0; i < instance.Length; i++)
            {
                if (context.Deserialize(out instance[i], Element)) continue;
                return false;
            }
            return true;
        }
    }
}