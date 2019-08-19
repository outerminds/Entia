using System;
using System.Reflection;
using Entia.Core;
using Entia.Serialization;

namespace Entia.Serializers
{
    public sealed class AbstractMember : Serializer<MemberInfo>
    {
        public override bool Serialize(in MemberInfo instance, in SerializeContext context)
        {
            context.Writer.Write(instance.MetadataToken);
            return context.Serialize(instance.DeclaringType, instance.DeclaringType.GetType());
        }

        public override bool Instantiate(out MemberInfo instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int token) &&
                context.Deserialize(out Type declaring) &&
                declaring.Member(token) is MemberInfo member)
            {
                instance = member;
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref MemberInfo instance, in DeserializeContext context) => true;
    }
}