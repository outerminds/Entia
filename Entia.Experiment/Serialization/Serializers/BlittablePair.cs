using Entia.Core;

namespace Entia.Experiment
{
    public sealed class BlittablePair<T> : Serializer<(T[] items, int count)> where T : unmanaged
    {
        public override bool Serialize(in (T[] items, int count) instance, in SerializeContext context)
        {
            context.Writer.Write(instance.count);
            context.Writer.Write(instance.items, instance.count);
            return true;
        }

        public override bool Instantiate(out (T[] items, int count) instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int count) && context.Reader.Read(out T[] items, count))
            {
                instance = (items, count);
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref (T[] items, int count) instance, in DeserializeContext context) => true;
    }
}