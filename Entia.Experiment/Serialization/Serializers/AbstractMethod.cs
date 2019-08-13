using System;
using System.Reflection;
using Entia.Core;

namespace Entia.Experiment
{
    public sealed class AbstractMethod : Serializer<MethodInfo>
    {
        public override bool Serialize(in MethodInfo instance, in SerializeContext context)
        {
            context.Writer.Write(instance.MetadataToken);
            if (context.Descriptors.Serialize(instance.DeclaringType, instance.DeclaringType.GetType(), context))
            {
                if (instance.IsGenericMethod)
                {
                    var arguments = instance.GetGenericArguments();
                    context.Writer.Write(arguments.Length);
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        var argument = arguments[i];
                        if (context.Descriptors.Serialize(argument, argument.GetType(), context)) continue;
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public override bool Instantiate(out MethodInfo instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int token) && context.Descriptors.Deserialize(out Type declaring, context))
            {
                instance = declaring.Method(token);
                if (instance.IsGenericMethodDefinition)
                {
                    if (context.Reader.Read(out int count))
                    {
                        var arguments = new Type[count];
                        for (int i = 0; i < arguments.Length; i++)
                        {
                            if (context.Descriptors.Deserialize(out arguments[i], context)) continue;
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