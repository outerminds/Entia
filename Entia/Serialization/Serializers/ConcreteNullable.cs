using Entia.Serialization;

namespace Entia.Serializers
{
    public sealed class ConcreteNullable<T> : Serializer<T?> where T : struct
    {
        public override bool Serialize(in T? instance, in SerializeContext context)
        {
            if (instance is T value)
            {
                context.Writer.Write(true);
                return context.Serialize(value);
            }
            else
            {
                context.Writer.Write(false);
                return true;
            }
        }

        public override bool Instantiate(out T? instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out bool has))
            {
                if (has)
                {
                    if (context.Deserialize(out T value)) { instance = value; return true; }
                }
                else { instance = default; return true; }
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref T? instance, in DeserializeContext context) => true;
    }
}