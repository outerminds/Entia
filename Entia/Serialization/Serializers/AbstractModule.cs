using System.Reflection;
using Entia.Experimental.Serialization;

namespace Entia.Experimental.Serializers
{
    public sealed class AbstractModule : Serializer<Module>
    {
        public override bool Serialize(in Module instance, in SerializeContext context) =>
            context.Serialize(instance.Assembly, instance.Assembly.GetType());

        public override bool Instantiate(out Module instance, in DeserializeContext context)
        {
            if (context.Deserialize(out Assembly assembly))
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