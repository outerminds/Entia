using System;
using Entia.Core;

namespace Entia.Experiment
{
    public sealed class ConcreteArray : Serializer<Array>
    {
        public readonly Type Type;
        public readonly ISerializer Element;

        public ConcreteArray(Type type, ISerializer element) { Type = type; Element = element; }

        public override bool Serialize(in Array instance, in SerializeContext context)
        {
            context.Writer.Write(instance.Length);
            for (int i = 0; i < instance.Length; i++)
            {
                if (Element.Serialize(instance.GetValue(i), context)) continue;
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
                if (Element.Deserialize(out var value, context)) instance.SetValue(value, i);
                else return false;
            }
            return true;
        }

        public override bool Clone(in Array instance, out Array clone, in CloneContext context)
        {
            clone = CloneUtility.Shallow(instance);
            for (int i = 0; i < clone.Length; i++)
            {
                if (Element.Clone(clone.GetValue(i), out var value, context)) clone.SetValue(value, i);
                else return false;
            }
            return true;
        }
    }

    public sealed class ConcreteArray<T> : Serializer<T[]>
    {
        public readonly Serializer<T> Element;

        public ConcreteArray(Serializer<T> element) { Element = element; }

        public override bool Serialize(in T[] instance, in SerializeContext context)
        {
            context.Writer.Write(instance.Length);
            for (int i = 0; i < instance.Length; i++)
            {
                if (Element.Serialize(instance[i], context)) continue;
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
                if (Element.Deserialize(out instance[i], context)) continue;
                return false;
            }
            return true;
        }

        public override bool Clone(in T[] instance, out T[] clone, in CloneContext context)
        {
            clone = CloneUtility.Shallow(instance);
            for (int i = 0; i < clone.Length; i++)
            {
                if (Element.Clone(clone[i], out clone[i], context)) continue;
                return false;
            }
            return true;
        }
    }
}