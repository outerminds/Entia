using System;
using System.Reflection;
using Entia.Core;

namespace Entia.Experiment
{
    public sealed class AbstractMethod : Serializer<MethodInfo>
    {
        public readonly Serializer<Type> Type;
        public AbstractMethod(Serializer<Type> type) { Type = type; }

        public override bool Serialize(in MethodInfo instance, in SerializeContext context)
        {
            context.Writer.Write(instance.MetadataToken);
            if (Type.Serialize(instance.DeclaringType, context))
            {
                if (instance.IsGenericMethod)
                {
                    var arguments = instance.GetGenericArguments();
                    context.Writer.Write(arguments.Length);
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        if (Type.Serialize(arguments[i], context)) continue;
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public override bool Instantiate(out MethodInfo instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int token) && Type.Deserialize(out var declaring, context))
            {
                instance = declaring.Method(token);
                if (instance.IsGenericMethodDefinition)
                {
                    if (context.Reader.Read(out int count))
                    {
                        var arguments = new Type[count];
                        for (int i = 0; i < arguments.Length; i++)
                        {
                            if (Type.Deserialize(out arguments[i], context)) continue;
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

        public override bool Clone(in MethodInfo instance, out MethodInfo clone, in CloneContext context)
        {
            clone = instance;
            return true;
        }
    }
}