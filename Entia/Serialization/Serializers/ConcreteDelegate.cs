using System;
using System.Reflection;
using Entia.Serialization;

namespace Entia.Serializers
{
    public sealed class ConcreteDelegate : Serializer<Delegate>
    {
        public override bool Serialize(in Delegate instance, in SerializeContext context)
        {
            var invocations = instance.GetInvocationList();
            context.Writer.Write(invocations.Length);
            if (invocations.Length == 1)
            {
                return
                    context.Serialize(instance.Method, instance.Method.GetType()) &&
                    context.Serialize(instance.Target);
            }
            else
            {
                for (int i = 0; i < invocations.Length; i++)
                {
                    if (Serialize(invocations[i], context)) continue;
                    return false;
                }
                return true;
            }
        }

        public override bool Instantiate(out Delegate instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int count))
            {
                if (count == 1)
                {
                    if (context.Deserialize(out MethodInfo method) && context.Deserialize(out object target))
                    {
                        instance = Delegate.CreateDelegate(context.Type, target, method);
                        return true;
                    }
                }
                else
                {
                    instance = default;
                    for (int i = 0; i < count; i++)
                    {
                        if (Deserialize(out var @delegate, context)) instance = Delegate.Combine(instance, @delegate);
                        else return false;
                    }
                    return true;
                }
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref Delegate instance, in DeserializeContext context) => true;
    }
}