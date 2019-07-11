using System.Reflection;
using Entia.Core;
using Entia.Modules;

namespace Entia.Serializers
{
    public sealed class Member : Serializer<MemberInfo>
    {
        public override bool Serialize(in MemberInfo instance, TypeData dynamic, TypeData @static, in WriteContext context)
        {
            var success = context.Serializers.Serialize(instance.Module, instance.Module.GetType(), context);
            context.Writer.Write(instance.MetadataToken);
            return success;
        }

        public override bool Instantiate(out MemberInfo instance, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            var success = context.Serializers.Deserialize(out System.Reflection.Module module, context);
            success &= context.Reader.Read(out int token);
            instance = module.ResolveMember(token);
            return success;
        }

        public override bool Deserialize(ref MemberInfo instance, TypeData dynamic, TypeData @static, in ReadContext context) => true;
    }
}