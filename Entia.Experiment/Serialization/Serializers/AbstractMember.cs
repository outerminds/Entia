using System.Reflection;

namespace Entia.Experiment
{
    public sealed class AbstractMember : Serializer<MemberInfo>
    {
        public override bool Serialize(in MemberInfo instance, in SerializeContext context)
        {
            context.Writer.Write(instance.MetadataToken);
            return context.Descriptors.Serialize(instance.Module, instance.Module.GetType(), context);
        }

        public override bool Instantiate(out MemberInfo instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int token) && context.Descriptors.Deserialize(out Module module, context))
            {
                instance = module.ResolveMember(token);
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref MemberInfo instance, in DeserializeContext context) => true;

        public override bool Clone(in MemberInfo instance, out MemberInfo clone, in CloneContext context)
        {
            clone = instance;
            return true;
        }
    }
}