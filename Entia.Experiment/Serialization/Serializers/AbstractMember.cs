using System.Reflection;
using Entia.Experiment.Serializationz;

namespace Entia.Experiment.Serializers
{
    public sealed class AbstractMember : Serializer<MemberInfo>
    {
        public override bool Serialize(in MemberInfo instance, in SerializeContext context)
        {
            context.Writer.Write(instance.MetadataToken);
            return context.Serialize(instance.Module, instance.Module.GetType());
        }

        public override bool Instantiate(out MemberInfo instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int token) && context.Deserialize(out Module module))
            {
                instance = module.ResolveMember(token);
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref MemberInfo instance, in DeserializeContext context) => true;
    }
}