using System;
using Entia.Core;

namespace Entia.Experiment
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
            context.Descriptors.Serialize(To(instance), context);

        public override bool Instantiate(out TFrom instance, in DeserializeContext context)
        {
            if (context.Descriptors.Deserialize(out TTo value, context))
            {
                instance = From(value);
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref TFrom instance, in DeserializeContext context) => true;

        public override bool Clone(in TFrom instance, out TFrom clone, in CloneContext context)
        {
            clone = From(To(instance));
            return true;
        }
    }
}