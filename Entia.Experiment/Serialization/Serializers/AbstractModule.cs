using System.Reflection;

namespace Entia.Experiment
{
    public sealed class AbstractModule : Serializer<Module>
    {
        public readonly Serializer<Assembly> Assembly;
        public AbstractModule(Serializer<Assembly> assembly) { Assembly = assembly; }

        public override bool Serialize(in Module instance, in SerializeContext context) =>
            Assembly.Serialize(instance.Assembly, context);

        public override bool Instantiate(out Module instance, in DeserializeContext context)
        {
            if (Assembly.Deserialize(out var assembly, context))
            {
                instance = assembly.ManifestModule;
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref Module instance, in DeserializeContext context) => true;

        public override bool Clone(in Module instance, out Module clone, in CloneContext context)
        {
            clone = instance;
            return true;
        }
    }
}