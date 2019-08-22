using Entia.Experimental.Serialization;

namespace Entia.Experimental.Serializers
{
    public sealed class ConcreteString : Serializer<string>
    {
        public override bool Serialize(in string instance, in SerializeContext context)
        {
            context.Writer.Write(instance);
            return true;
        }

        public override bool Instantiate(out string instance, in DeserializeContext context) => context.Reader.Read(out instance);
        public override bool Initialize(ref string instance, in DeserializeContext context) => true;
    }
}