using System;
using System.Runtime.Serialization;
using Entia.Experiment.Serializationz;

namespace Entia.Experiment.Serializers
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
    }

    public sealed class ConcreteObject<T> : Serializer<T>
    {
        static readonly Func<T> _instantiate = typeof(T).IsValueType ?
            new Func<T>(() => default) :
            new Func<T>(() => (T)FormatterServices.GetUninitializedObject(typeof(T)));

        public readonly Func<T> Construct = _instantiate;
        public readonly IMember<T>[] Members;

        public ConcreteObject(Func<T> construct, params IMember<T>[] members) { Construct = construct; Members = members; }
        public ConcreteObject(params IMember<T>[] members) { Members = members; }

        public override bool Serialize(in T instance, in SerializeContext context)
        {
            // TODO: tolerant deserialize
            for (int i = 0; i < Members.Length; i++)
            {
                // var member = Members[i];
                // var next = context.Writer.Reserve<int>();
                // if (member.Serialize(instance, context))
                // {
                //     next.Value = context.Writer.Position;
                //     continue;
                // }
                // return false;
                if (Members[i].Serialize(instance, context)) continue;
                return false;
            }
            return true;
        }

        public override bool Instantiate(out T instance, in DeserializeContext context)
        {
            instance = Construct();
            return true;
        }

        public override bool Initialize(ref T instance, in DeserializeContext context)
        {
            for (int i = 0; i < Members.Length; i++)
            {
                // var member = Members[i];
                // context.Reader.Read(out int next);
                // member.Deserialize(ref instance, context);
                // context.Reader.Position = next;
                if (Members[i].Deserialize(ref instance, context)) continue;
                return false;
            }
            return true;
        }
    }
}