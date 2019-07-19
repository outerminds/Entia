using Entia.Core;
using Entia.Modules;

namespace Entia.Serializers
{
    public sealed class ReflectionModule : Serializer<System.Reflection.Module>
    {
        public override bool Serialize(in System.Reflection.Module instance, TypeData dynamic, TypeData @static, in WriteContext context) =>
            context.Serializers.Serialize(instance.Assembly, instance.Assembly.GetType(), context);

        public override bool Instantiate(out System.Reflection.Module instance, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            var success = context.Serializers.Deserialize(out System.Reflection.Assembly assembly, context);
            instance = assembly.ManifestModule;
            return success;
        }

        public override bool Deserialize(ref System.Reflection.Module instance, TypeData dynamic, TypeData @static, in ReadContext context) => true;
    }
}