using System;

namespace Entia.Experiment
{
    public sealed class AbstractObject : ISerializer
    {
        enum Kinds : byte { None, Null, Abstract, Concrete }

        public readonly Type Type;
        public readonly (Serializer<Type> type, ISerializer concrete) Serializers;

        public AbstractObject(Type type, (Serializer<Type> type, ISerializer concrete) serializers)
        {
            Type = type;
            Serializers = serializers;
        }

        public bool Serialize(object instance, in SerializeContext context)
        {
            if (instance is null)
            {
                context.Writer.Write(Kinds.Null);
                return true;
            }

            var type = instance.GetType();
            if (type == Type)
            {
                context.Writer.Write(Kinds.Concrete);
                return Serializers.concrete.Serialize(instance, context);
            }
            else
            {
                context.Writer.Write(Kinds.Abstract);
                return Serializers.type.Serialize(type, context) && context.Descriptors.Serialize(instance, type, context);
            }
        }

        public bool Instantiate(out object instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out Kinds kind))
            {
                switch (kind)
                {
                    case Kinds.Null: instance = null; return true;
                    case Kinds.Concrete: return Serializers.concrete.Deserialize(out instance, context);
                    case Kinds.Abstract:
                        if (Serializers.type.Deserialize(out var type, context))
                            return context.Descriptors.Deserialize(out instance, type, context);
                        break;
                }
            }
            instance = default;
            return false;
        }

        public bool Initialize(ref object instance, in DeserializeContext context) => true;

        public bool Clone(object instance, out object clone, in CloneContext context)
        {
            var type = instance.GetType();
            if (type == Type) return Serializers.concrete.Clone(instance, out clone, context);
            else return context.Descriptors.Clone(instance, out clone, context);
        }
    }
}