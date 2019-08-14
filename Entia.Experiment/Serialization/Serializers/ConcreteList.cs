using System;
using System.Collections;
using System.Collections.Generic;
using Entia.Experiment.Serializationz;

namespace Entia.Experiment.Serializers
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
                if (context.Serialize(instance[i], Type)) continue;
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
                    if (context.Deserialize(out var value, Type)) instance.Add(value);
                    else return false;
                }
                return true;
            }
            return false;
        }
    }

    public sealed class ConcreteList<T> : Serializer<List<T>>
    {
        public override bool Serialize(in List<T> instance, in SerializeContext context) =>
            context.Serialize(instance.ToArray());

        public override bool Instantiate(out List<T> instance, in DeserializeContext context)
        {
            instance = new List<T>();
            return true;
        }

        public override bool Initialize(ref List<T> instance, in DeserializeContext context)
        {
            if (context.Deserialize(out T[] values))
            {
                instance.AddRange(values);
                return true;
            }
            return false;
        }
    }
}