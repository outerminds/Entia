using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Experiment
{
    public sealed class ConcreteList : Serializer<IList>
    {
        public readonly Type Type;
        public ConcreteList(Type type) { Type = type; }

        static IList Instantiate(Type type, int capacity = 0) => Activator.CreateInstance(typeof(List<>).MakeGenericType(type), capacity) as IList;

        public override bool Serialize(in IList instance, in SerializeContext context)
        {
            context.Writer.Write(instance.Count);
            for (int i = 0; i < instance.Count; i++)
            {
                if (context.Descriptors.Serialize(instance[i], Type, context)) continue;
                return false;
            }
            return true;
        }

        public override bool Instantiate(out IList instance, in DeserializeContext context)
        {
            instance = Instantiate(Type);
            return true;
        }

        public override bool Initialize(ref IList instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int count))
            {
                for (int i = 0; i < count; i++)
                {
                    if (context.Descriptors.Deserialize(out var value, Type, context)) instance.Add(value);
                    else return false;
                }
                return true;
            }
            return false;
        }

        public override bool Clone(in IList instance, out IList clone, in CloneContext context)
        {
            clone = Instantiate(Type, instance.Count);
            for (int i = 0; i < instance.Count; i++)
            {
                if (context.Descriptors.Clone(instance[i], out var value, Type, context)) clone[i] = value;
                else return false;
            }
            return true;
        }
    }

    public sealed class ConcreteList<T> : Serializer<List<T>>
    {
        public override bool Serialize(in List<T> instance, in SerializeContext context) =>
            context.Descriptors.Serialize(instance.ToArray(), context);

        public override bool Instantiate(out List<T> instance, in DeserializeContext context)
        {
            instance = new List<T>();
            return true;
        }

        public override bool Initialize(ref List<T> instance, in DeserializeContext context)
        {
            if (context.Descriptors.Deserialize(out T[] values, context))
            {
                instance.AddRange(values);
                return true;
            }
            return false;
        }

        public override bool Clone(in List<T> instance, out List<T> clone, in CloneContext context)
        {
            clone = new List<T>(instance.Count);
            for (int i = 0; i < instance.Count; i++)
            {
                if (context.Descriptors.Clone(instance[i], out var value, context)) clone.Add(value);
                else return false;
            }
            return true;
        }
    }
}