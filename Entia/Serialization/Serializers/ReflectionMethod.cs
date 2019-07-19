using System;
using System.Reflection;
using Entia.Core;
using Entia.Modules;

namespace Entia.Serializers
{
    public sealed class ReflectionMethod : Serializer<MethodInfo>
    {
        public override bool Serialize(in MethodInfo instance, TypeData dynamic, TypeData @static, in WriteContext context)
        {
            var success = context.Serializers.Serialize(instance.DeclaringType, instance.DeclaringType.GetType(), context);
            context.Writer.Write(instance.MetadataToken);

            if (instance.IsGenericMethod)
            {
                var arguments = instance.GetGenericArguments();
                context.Writer.Write(arguments.Length);
                for (int i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    success &= context.Serializers.Serialize(argument, argument.GetType(), context);
                }
            }
            return success;
        }

        public override bool Instantiate(out MethodInfo instance, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            var success = context.Serializers.Deserialize(out Type declaring, context);
            success &= context.Reader.Read(out int token);

            instance = declaring.Method(token);
            if (instance.IsGenericMethodDefinition)
            {
                success &= context.Reader.Read(out int count);
                var arguments = new Type[count];
                for (int i = 0; i < arguments.Length; i++)
                {
                    success &= context.Serializers.Deserialize(out Type argument, context);
                    arguments[i] = argument;
                }
                instance = instance.MakeGenericMethod(arguments);
            }
            return success;
        }

        public override bool Deserialize(ref MethodInfo instance, TypeData dynamic, TypeData @static, in ReadContext context) => true;
    }
}