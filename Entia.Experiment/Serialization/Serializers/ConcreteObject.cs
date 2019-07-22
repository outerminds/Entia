using System;
using System.Runtime.Serialization;
using Entia.Core;

namespace Entia.Experiment
{
    public sealed class ConcreteObject : ISerializer
    {
        public readonly Type Type;
        public readonly IMember[] Members;

        public ConcreteObject(Type type, params IMember[] members) { Type = type; Members = members; }

        public bool Serialize(object instance, in SerializeContext context)
        {
            for (int i = 0; i < Members.Length; i++)
            {
                if (Members[i].Serialize(instance, context)) continue;
                return false;
            }
            return true;
        }

        public bool Instantiate(out object instance, in DeserializeContext context)
        {
            instance = FormatterServices.GetUninitializedObject(Type);
            return true;
        }

        public bool Initialize(ref object instance, in DeserializeContext context)
        {
            for (int i = 0; i < Members.Length; i++)
            {
                if (Members[i].Deserialize(instance, context)) continue;
                else return false;
            }
            return true;
        }

        public bool Clone(object instance, out object clone, in CloneContext context)
        {
            clone = CloneUtility.Shallow(instance);
            for (int i = 0; i < Members.Length; i++)
            {
                if (Members[i].Clone(instance, ref clone, context)) continue;
                else return false;
            }
            return true;
        }
    }

    public sealed class ConcreteObject<T> : Serializer<T>
    {
        static readonly Func<T> _instantiate = typeof(T).IsValueType ?
            new Func<T>(() => default) :
            new Func<T>(() => (T)FormatterServices.GetUninitializedObject(typeof(T)));

        public readonly IMember<T>[] Members;

        public ConcreteObject(params IMember<T>[] members) { Members = members; }

        public override bool Serialize(in T instance, in SerializeContext context)
        {
            for (int i = 0; i < Members.Length; i++)
            {
                if (Members[i].Serialize(instance, context)) continue;
                return false;
            }
            return true;
        }

        public override bool Instantiate(out T instance, in DeserializeContext context)
        {
            instance = _instantiate();
            return true;
        }

        public override bool Initialize(ref T instance, in DeserializeContext context)
        {
            for (int i = 0; i < Members.Length; i++)
            {
                if (Members[i].Deserialize(instance, context)) continue;
                return false;
            }
            return true;
        }

        public override bool Clone(in T instance, out T clone, in CloneContext context)
        {
            clone = CloneUtility.Shallow(instance);
            for (int i = 0; i < Members.Length; i++)
            {
                if (Members[i].Clone(instance, ref clone, context)) continue;
                return false;
            }
            return true;
        }
    }
}