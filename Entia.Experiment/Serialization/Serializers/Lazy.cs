using System;

namespace Entia.Experiment
{
    public sealed class Lazy : ISerializer
    {
        public Type Type;
        public Lazy(Type type) { Type = type; }

        public bool Serialize(object instance, in SerializeContext context) =>
            context.Descriptors.Serialize(instance, Type, context);

        public bool Instantiate(out object instance, in DeserializeContext context) =>
            context.Descriptors.Deserialize(out instance, Type, context);

        public bool Initialize(ref object instance, in DeserializeContext context) => true;

        public bool Clone(object instance, out object clone, in CloneContext context) =>
            context.Descriptors.Clone(instance, out clone, Type, context);
    }

    public sealed class Lazy<T> : Serializer<T>
    {
        ISerializer _serializer;

        public override bool Serialize(in T instance, in SerializeContext context) =>
            Get(context.Descriptors).Serialize(instance, context);

        public override bool Instantiate(out T instance, in DeserializeContext context) =>
            Get(context.Descriptors).Instantiate(out instance, context);

        public override bool Initialize(ref T instance, in DeserializeContext context) =>
            Get(context.Descriptors).Initialize(ref instance, context);

        public override bool Clone(in T instance, out T clone, in CloneContext context) =>
            Get(context.Descriptors).Clone(instance, out clone, context);

        ISerializer Get(Descriptors descriptors) => _serializer ?? (_serializer = descriptors.Describe<T>());
    }
}