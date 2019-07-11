using Entia.Core;
using Entia.Modules;

namespace Entia.Serializers
{
    public sealed class Array : Serializer<System.Array>
    {
        public override bool Serialize(in System.Array instance, TypeData dynamic, TypeData @static, in WriteContext context)
        {
            context.Writer.Write(instance.Length);
            if (instance.Length <= 0) return true;

            var element = TypeUtility.GetData(@static.Element);
            for (int i = 0; i < instance.Length; i++) context.Serializers.Serialize(instance.GetValue(i), element, context);
            return true;
        }

        public override bool Instantiate(out System.Array instance, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            var success = context.Reader.Read(out int count);
            instance = System.Array.CreateInstance(dynamic.Element, count);
            return success;
        }

        public override bool Deserialize(ref System.Array instance, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            var success = true;
            if (instance.Length > 0)
            {
                var element = TypeUtility.GetData(dynamic.Element);
                for (int i = 0; i < instance.Length; i++)
                {
                    if (context.Serializers.Deserialize(out var item, element, context)) instance.SetValue(item, i);
                    else success = false;
                }
            }
            return success;
        }
    }
}