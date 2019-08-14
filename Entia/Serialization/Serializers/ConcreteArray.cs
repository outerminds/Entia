using System;
using Entia.Serialization;

namespace Entia.Serializers
{
    public sealed class ConcreteArray : Serializer<Array>
    {
        public readonly Type Type;

        public ConcreteArray(Type type) { Type = type; }

        public override bool Serialize(in Array instance, in SerializeContext context)
        {
            context.Writer.Write(instance.Length);
            for (int i = 0; i < instance.Length; i++)
            {
                if (context.Serialize(instance.GetValue(i), Type)) continue;
                return false;
            }
            return true;
        }

        public override bool Instantiate(out Array instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int count))
            {
                instance = Array.CreateInstance(Type, count);
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref Array instance, in DeserializeContext context)
        {
            for (int i = 0; i < instance.Length; i++)
            {
                if (context.Deserialize(out var value, Type)) instance.SetValue(value, i);
                else return false;
            }
            return true;
        }
    }

    public sealed class ConcreteArray<T> : Serializer<T[]>
    {
        public readonly Serializer<T> Element;
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