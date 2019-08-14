using System;
using System.Reflection;
using Entia.Core;
using Entia.Serialization;

namespace Entia.Serializers
{
    public sealed class AbstractMethod : Serializer<MethodInfo>
    {
        public override bool Serialize(in MethodInfo instance, in SerializeContext context)
        {
            context.Writer.Write(instance.MetadataToken);
            if (context.Serialize(instance.DeclaringType, instance.DeclaringType.GetType()))
            {
                if (instance.IsGenericMethod)
                {
                    var arguments = instance.GetGenericArguments();
                    context.Writer.Write(arguments.Length);
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        var argument = arguments[i];
                        if (context.Serialize(argument, argument.GetType())) continue;
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public override bool Instantiate(out MethodInfo instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int token) && context.Deserialize(out Type declaring))
            {
                instance = declaring.Method(token);
                if (instance.IsGenericMethodDefinition)
                {
                    if (context.Reader.Read(out int count))
                    {
                        var arguments = new Type[count];
                        for (int i = 0; i < arguments.Length; i++)
                        {
                            if (context.Deserialize(out arguments[i])) continue;
                            return false;
                        }
                        instance = instance.MakeGenericMethod(arguments);
                    }
                    else return false;
                }
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref MethodInfo instance, in DeserializeContext context) => true;
    }
}