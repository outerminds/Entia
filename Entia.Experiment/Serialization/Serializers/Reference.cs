using Entia.Core;

namespace Entia.Experiment
{
    public sealed class Reference : ISerializer
    {
        public enum Kinds : byte { None, Null, Value, Reference }

        public readonly ISerializer Serializer;

        public Reference(ISerializer serializer) { Serializer = serializer; }

        public bool Serialize(object instance, in SerializeContext context)
        {
            if (instance is null)
            {
                context.Writer.Write(Kinds.Null);
                return true;
            }
            else if (context.References.TryGetValue(instance, out var index))
            {
                context.Writer.Write(Kinds.Reference);
                context.Writer.Write((ushort)index);
                return true;
            }
            else
            {
                context.Writer.Write(Kinds.Value);
                context.References[instance] = context.References.Count;
                return Serializer.Serialize(instance, context);
            }
        }

        public bool Instantiate(out object instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out Kinds kind))
            {
                switch (kind)
                {
                    case Kinds.Null: instance = default; return true;
                    case Kinds.Value:
                        {
                            var index = context.References.Count;
                            context.References.Add(default);
                            if (Serializer.Instantiate(out instance, context))
                            {
                                context.References[index] = instance;
                                if (Serializer.Initialize(ref instance, context))
                                {
                                    context.References[index] = instance;
                                    return true;
                                }
                                return false;
                            }
                            return false;
                        }
                    case Kinds.Reference:
                        {
                            context.Reader.Read(out ushort index);
                            instance = context.References[index];
                            return true;
                        }
                }
            }
            instance = default;
            return false;
        }

        public bool Initialize(ref object instance, in DeserializeContext context) => true;

        public bool Clone(object instance, out object clone, in CloneContext context) =>
            Serializer.Clone(instance, out clone, context);
    }

    public sealed class Reference<T> : Serializer<T>
    {
        public enum Kinds : byte { None, Null, Value, Reference }

        static readonly InFunc<T, bool> _isNull = typeof(T).IsValueType ?
            new InFunc<T, bool>((in T _) => false) :
            new InFunc<T, bool>((in T value) => value == null);

        public readonly Serializer<T> Serializer;

        public Reference(Serializer<T> serializer) { Serializer = serializer; }

        public override bool Serialize(in T instance, in SerializeContext context)
        {
            if (_isNull(instance))
            {
                context.Writer.Write(Kinds.Null);
                return true;
            }
            else if (context.References.TryGetValue(instance, out var index))
            {
                context.Writer.Write(Kinds.Reference);
                context.Writer.Write((ushort)index);
                return true;
            }
            else
            {
                context.Writer.Write(Kinds.Value);
                context.References[instance] = context.References.Count;
                return Serializer.Serialize(instance, context);
            }
        }

        public override bool Instantiate(out T instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out Kinds kind))
            {
                switch (kind)
                {
                    case Kinds.Null: instance = default; return true;
                    case Kinds.Value:
                        {
                            var index = context.References.Count;
                            context.References.Add(default(T));
                            if (Serializer.Instantiate(out instance, context))
                            {
                                context.References[index] = instance;
                                if (Serializer.Initialize(ref instance, context))
                                {
                                    context.References[index] = instance;
                                    return true;
                                }
                                return false;
                            }
                            return false;
                        }
                    case Kinds.Reference:
                        {
                            context.Reader.Read(out ushort index);
                            instance = (T)context.References[index];
                            return true;
                        }
                }
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref T instance, in DeserializeContext context) => true;

        public override bool Clone(in T instance, out T clone, in CloneContext context) =>
            Serializer.Clone(instance, out clone, context);
    }
}