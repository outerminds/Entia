using System;
using Entia.Core;

namespace Entia.Experiment
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
                if (context.Descriptors.Serialize(instance.GetValue(i), Type, context)) continue;
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
                if (context.Descriptors.Deserialize(out var value, Type, context)) instance.SetValue(value, i);
                else return false;
            }
            return true;
        }
    }

    public sealed class ConcreteArray<T> : Serializer<T[]>
    {
        public override bool Serialize(in T[] instance, in SerializeContext context)
        {
            context.Writer.Write(instance.Length);
            for (int i = 0; i < instance.Length; i++)
            {
                if (context.Descriptors.Serialize(instance[i], context)) continue;
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
                if (context.Descriptors.Deserialize(out instance[i], context)) continue;
                return false;
            }
            return true;
        }
    }
}