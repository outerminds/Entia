using Entia.Experimental.Serialization;

namespace Entia.Experimental.Serializers
{
    public sealed class ConcreteNullable<T> : Serializer<T?> where T : struct
    {
        public readonly Serializer<T> Value;

        public ConcreteNullable() { }
        public ConcreteNullable(Serializer<T> value = null) { Value = value; }

        public override bool Serialize(in T? instance, in SerializeContext context)
        {
            if (instance is T value)
            {
                context.Writer.Write(true);
                return context.Serialize(value, Value);
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
                    if (context.Deserialize(out T value, Value)) { instance = value; return true; }
                }
                else { instance = default; return true; }
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref T? instance, in DeserializeContext context) => true;
    }
}