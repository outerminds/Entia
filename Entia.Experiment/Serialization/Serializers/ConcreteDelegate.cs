using System;
using System.Reflection;
using Entia.Core;

namespace Entia.Experiment
{
    public sealed class ConcreteDelegate : Serializer<Delegate>
    {
        public readonly Type Type;
        public readonly (Serializer<MethodInfo> method, ISerializer target, Serializer<Delegate> @delegate) Serializers;

        public ConcreteDelegate(Type type, Serializer<MethodInfo> method, ISerializer target)
        {
            Type = type;
            Serializers = (method, target, Serializer.Reference(this));
        }

        public override bool Serialize(in Delegate instance, in SerializeContext context)
        {
            var invocations = instance.GetInvocationList();
            context.Writer.Write(invocations.Length);
            if (invocations.Length == 1)
            {
                return
                    Serializers.method.Serialize(instance.Method, context) &&
                    Serializers.target.Serialize(instance.Target, context);
            }
            else
            {
                for (int i = 0; i < invocations.Length; i++)
                {
                    if (Serializers.@delegate.Serialize(invocations[i], context)) continue;
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
                    if (Serializers.method.Deserialize(out var method, context) &&
                        Serializers.target.Deserialize(out var target, context))
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
                        if (Serializers.@delegate.Deserialize(out var @delegate, context))
                            instance = Delegate.Combine(instance, @delegate);
                        else return false;
                    }
                    return true;
                }
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref Delegate instance, in DeserializeContext context) => true;

        public override bool Clone(in Delegate instance, out Delegate clone, in CloneContext context)
        {
            clone = CloneUtility.Shallow(instance);
            return true;
        }
    }

    public sealed class ConcreteDelegate<T> : Serializer<T> where T : Delegate
    {
        public readonly (Serializer<MethodInfo> method, ISerializer target, Serializer<T> @delegate) Serializers;

        public ConcreteDelegate(Serializer<MethodInfo> method, ISerializer target)
        {
            Serializers = (method, target, Serializer.Reference(this));
        }

        public override bool Serialize(in T instance, in SerializeContext context)
        {
            var invocations = instance.GetInvocationList();
            context.Writer.Write(invocations.Length);
            if (invocations.Length == 1)
            {
                return
                    Serializers.method.Serialize(instance.Method, context) &&
                    Serializers.target.Serialize(instance.Target, context);
            }
            else
            {
                for (int i = 0; i < invocations.Length; i++)
                {
                    if (Serializers.@delegate.Serialize((T)invocations[i], context)) continue;
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
                    if (Serializers.method.Deserialize(out var method, context) &&
                        Serializers.target.Deserialize(out var target, context))
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
                        if (Serializers.@delegate.Deserialize(out var @delegate, context))
                            instance = (T)Delegate.Combine(instance, @delegate);
                        else return false;
                    }
                    return true;
                }
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref T instance, in DeserializeContext context) => true;

        public override bool Clone(in T instance, out T clone, in CloneContext context)
        {
            clone = CloneUtility.Shallow(instance);
            return true;
        }
    }
}