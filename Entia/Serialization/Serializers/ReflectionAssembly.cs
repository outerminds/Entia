using Entia.Core;
using Entia.Modules;

namespace Entia.Serializers
{
    public sealed class ReflectionAssembly : Serializer<System.Reflection.Assembly>
    {
        public override bool Serialize(in System.Reflection.Assembly instance, TypeData dynamic, TypeData @static, in WriteContext context)
        {
            context.Writer.Write(instance.GetName().Name);
            return true;
        }

        public override bool Instantiate(out System.Reflection.Assembly instance, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            var success = context.Reader.Read(out string name);
            instance = System.Reflection.Assembly.Load(name);
            return success;
        }

        public override bool Deserialize(ref System.Reflection.Assembly instance, TypeData dynamic, TypeData @static, in ReadContext context) => true;
    }
}