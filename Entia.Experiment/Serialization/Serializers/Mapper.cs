using Entia.Core;
using Entia.Experiment.Serializationz;

namespace Entia.Experiment.Serializers
{
    public sealed class Mapper<TFrom, TTo> : Serializer<TFrom>
    {
        public readonly InFunc<TFrom, TTo> To;
        public readonly InFunc<TTo, TFrom> From;
        public readonly Serializer<TTo> Serializer;

        public Mapper(InFunc<TFrom, TTo> to, InFunc<TTo, TFrom> from, Serializer<TTo> serializer = null)
        {
            To = to;
            From = from;
            Serializer = serializer;
        }

        public override bool Serialize(in TFrom instance, in SerializeContext context) =>
            Serializer is null ? context.Serialize(To(instance)) : Serializer.Serialize(To(instance), context);

        public override bool Instantiate(out TFrom instance, in DeserializeContext context)
        {
            if (Serializer is null ? context.Deserialize(out TTo value) : Serializer.Deserialize(out value, context))
            {
                instance = From(value);
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref TFrom instance, in DeserializeContext context) => true;
    }
}