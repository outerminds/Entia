using Entia.Core;
using Entia.Experiment.Serializationz;

namespace Entia.Experiment.Serializers
{
    public sealed class Mapper<TFrom, TTo> : Serializer<TFrom>
    {
        public readonly InFunc<TFrom, TTo> To;
        public readonly InFunc<TTo, TFrom> From;

        public Mapper(InFunc<TFrom, TTo> to, InFunc<TTo, TFrom> from)
        {
            To = to;
            From = from;
        }

        public override bool Serialize(in TFrom instance, in SerializeContext context) =>
            context.Serialize(To(instance));

        public override bool Instantiate(out TFrom instance, in DeserializeContext context)
        {
            if (context.Deserialize(out TTo value))
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