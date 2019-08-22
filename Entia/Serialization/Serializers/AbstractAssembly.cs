using System.Reflection;
using Entia.Experimental.Serialization;

namespace Entia.Experimental.Serializers
{
    public sealed class AbstractAssembly : Serializer<Assembly>
    {
        public override bool Serialize(in Assembly instance, in SerializeContext context)
        {
            context.Writer.Write(instance.GetName().Name);
            return true;
        }

        public override bool Instantiate(out Assembly instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out string name))
            {
                instance = Assembly.Load(name);
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref Assembly instance, in DeserializeContext context) => true;
    }
}