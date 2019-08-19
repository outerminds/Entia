using System.Collections.Generic;
using Entia.Serialization;

namespace Entia.Serializers
{
    public sealed class ConcreteList<T> : Serializer<List<T>>
    {
        public readonly Serializer<T[]> Values;

        public ConcreteList() { }
        public ConcreteList(Serializer<T[]> values = null) { Values = values; }

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