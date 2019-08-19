using Entia.Serialization;

namespace Entia.Serializers
{
    public sealed class BlittableArray<T> : Serializer<T[]> where T : unmanaged
    {
        public override bool Serialize(in T[] instance, in SerializeContext context)
        {
            context.Writer.Write(instance.Length);
            context.Writer.Write(instance);
            return true;
        }

        public override bool Instantiate(out T[] instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int count) && context.Reader.Read(out instance, count)) return true;
            instance = default;
            return false;
        }

        public override bool Initialize(ref T[] instance, in DeserializeContext context) => true;
    }
}