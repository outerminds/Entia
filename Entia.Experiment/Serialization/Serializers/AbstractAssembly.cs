using System.Reflection;

namespace Entia.Experiment
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

        public override bool Clone(in Assembly instance, out Assembly clone, in CloneContext context)
        {
            clone = instance;
            return true;
        }
    }
}