using System;
using Entia.Core;
using Entia.Experimental.Serialization;

namespace Entia.Experimental.Serializers
{
    public sealed class ConcreteBox : Serializer<IBox>
    {
        public override bool Serialize(in IBox instance, in SerializeContext context) => context.Serialize(instance.Box);

        public override bool Instantiate(out IBox instance, in DeserializeContext context)
        {
            if (context.Deserialize(out Array box))
            {
                instance = (IBox)Activator.CreateInstance(context.Type, box);
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref IBox instance, in DeserializeContext context) => true;
    }
}