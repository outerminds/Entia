using System.Reflection;

namespace Entia.Experiment
{
    public sealed class AbstractModule : Serializer<Module>
    {
        public override bool Serialize(in Module instance, in SerializeContext context) =>
            context.Descriptors.Serialize(instance.Assembly, instance.Assembly.GetType(), context);

        public override bool Instantiate(out Module instance, in DeserializeContext context)
        {
            if (context.Descriptors.Deserialize(out Assembly assembly, context))
            {
                instance = assembly.ManifestModule;
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref Module instance, in DeserializeContext context) => true;
    }
}