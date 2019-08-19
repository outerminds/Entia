using Entia.Serialization;

namespace Entia.Serializers
{
    public sealed class Any : ISerializer
    {
        public readonly ISerializer[] Serializers;

        public Any(params ISerializer[] serializers) { Serializers = serializers; }

        public bool Serialize(object instance, in SerializeContext context)
        {
            var position = context.Writer.Position;
            for (int i = 0; i < Serializers.Length; i++)
            {
                if (context.Serialize(instance, Serializers[i])) return true;
                context.Writer.Position = position;
            }
            return context.Serialize(instance);
        }

        public bool Instantiate(out object instance, in DeserializeContext context)
        {
            var position = context.Reader.Position;
            for (int i = 0; i < Serializers.Length; i++)
            {
                if (context.Deserialize(out instance, Serializers[i])) return true;
                context.Reader.Position = position;
            }
            return context.Deserialize(out instance);
        }

        public bool Initialize(ref object instance, in DeserializeContext context) => true;
    }

    public sealed class Any<T> : Serializer<T>
    {
        public readonly Serializer<T>[] Serializers;

        public Any(params Serializer<T>[] serializers) { Serializers = serializers; }

        public override bool Serialize(in T instance, in SerializeContext context)
        {
            var position = context.Writer.Position;
            for (int i = 0; i < Serializers.Length; i++)
            {
                if (context.Serialize(instance, Serializers[i])) return true;
                context.Writer.Position = position;
            }
            return context.Serialize(instance);
        }

        public override bool Instantiate(out T instance, in DeserializeContext context)
        {
            var position = context.Reader.Position;
            for (int i = 0; i < Serializers.Length; i++)
            {
                if (context.Deserialize(out instance, Serializers[i])) return true;
                context.Reader.Position = position;
            }
            return context.Deserialize(out instance);
        }

        public override bool Initialize(ref T instance, in DeserializeContext context) => true;
    }
}