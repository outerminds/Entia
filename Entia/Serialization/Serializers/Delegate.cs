using System.Reflection;
using Entia.Core;
using Entia.Modules;

namespace Entia.Serializers
{
    public sealed class Delegate : Serializer<System.Delegate>
    {
        public override bool Serialize(in System.Delegate instance, TypeData dynamic, TypeData @static, in WriteContext context)
        {
            var invocations = instance.GetInvocationList();
            var success = true;
            context.Writer.Write(invocations.Length);
            if (invocations.Length == 1)
            {
                success &= context.Serializers.Serialize(instance.Method, context);
                success &= context.Serializers.Serialize(instance.Target, context);
            }
            else
            {
                for (int i = 0; i < invocations.Length; i++)
                    success &= context.Serializers.Serialize(invocations[i], dynamic, context);
            }
            return success;
        }

        public override bool Instantiate(out System.Delegate instance, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            var success = context.Reader.Read(out int count);
            if (count == 1)
            {
                success &= context.Serializers.Deserialize(out MethodInfo method, context);
                success &= context.Serializers.Deserialize(out object target, context);
                instance = System.Delegate.CreateDelegate(dynamic, target, method);
            }
            else
            {
                instance = default;
                for (int i = 0; i < count; i++)
                {
                    success &= context.Serializers.Deserialize(out var @delegate, dynamic, context);
                    instance = System.Delegate.Combine(instance, @delegate as System.Delegate);
                }
            }
            return success;
        }

        public override bool Deserialize(ref System.Delegate instance, TypeData dynamic, TypeData @static, in ReadContext context) => true;
    }
}