using Entia.Serialization;

namespace Entia.Serializers
{
    public sealed class BlittableObject<T> : Serializer<T> where T : unmanaged
    {
        public override bool Serialize(in T instance, in SerializeContext context)
        {
            context.Writer.Write(instance);
            return true;
        }

        public override bool Instantiate(out T instance, in DeserializeContext context) => context.Reader.Read(out instance);
        public override bool Initialize(ref T instance, in DeserializeContext context) => true;
    }
}