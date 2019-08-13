using System;
using System.Reflection;
using Entia.Core;

namespace Entia.Experiment
{
    public sealed class ConcreteDelegate : Serializer<Delegate>
    {
        public readonly Type Type;

        public ConcreteDelegate(Type type) { Type = type; }

        public override bool Serialize(in Delegate instance, in SerializeContext context)
        {
            var invocations = instance.GetInvocationList();
            context.Writer.Write(invocations.Length);
            if (invocations.Length == 1)
            {
                return
                    context.Descriptors.Serialize(instance.Method, instance.Method.GetType(), context) &&
                    context.Descriptors.Serialize(instance.Target, context);
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
                    if (context.Descriptors.Deserialize(out MethodInfo method, context) &&
                        context.Descriptors.Deserialize(out object target, context))
                    {
                        instance = Delegate.CreateDelegate(Type, target, method);
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

    public sealed class ConcreteDelegate<T> : Serializer<T> where T : Delegate
    {
        public override bool Serialize(in T instance, in SerializeContext context)
        {
            var invocations = instance.GetInvocationList();
            context.Writer.Write(invocations.Length);
            if (invocations.Length == 1)
            {
                return
                    context.Descriptors.Serialize(instance.Method, instance.Method.GetType(), context) &&
                    context.Descriptors.Serialize(instance.Target, context);
            }
            else
            {
                for (int i = 0; i < invocations.Length; i++)
                {
                    if (Serialize((T)invocations[i], context)) continue;
                    return false;
                }
                return true;
            }
        }

        public override bool Instantiate(out T instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int count))
            {
                if (count == 1)
                {
                    if (context.Descriptors.Deserialize(out MethodInfo method, context) &&
                        context.Descriptors.Deserialize(out object target, context))
                    {
                        instance = (T)Delegate.CreateDelegate(typeof(T), target, method);
                        return true;
                    }
                }
                else
                {
                    instance = default;
                    for (int i = 0; i < count; i++)
                    {
                        if (Deserialize(out var @delegate, context)) instance = (T)Delegate.Combine(instance, @delegate);
                        else return false;
                    }
                    return true;
                }
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref T instance, in DeserializeContext context) => true;
    }
}