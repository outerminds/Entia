using System.Runtime.InteropServices;
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

            var success = true;
            var element = TypeUtility.GetData(dynamic.Element);
            if (element.IsBlittable)
            {
                var serializer = context.Serializers.Get(element);
                var position = context.Writer.Position;
                context.Writer.Write((ushort)0);
                var begin = context.Writer.Position;
                for (int i = 0; i < instance.Length; i++)
                    success |= serializer.Serialize(instance.GetValue(i), element, element, context);
                var end = context.Writer.Position;
                context.Writer.Position = position;
                context.Writer.Write((ushort)((end - begin) / instance.Length));
                context.Writer.Position = end;
            }
            else if (element.Type.IsSealed)
            {
                var serializer = context.Serializers.Get(element);
                for (int i = 0; i < instance.Length; i++)
                    success |= serializer.Serialize(instance.GetValue(i), element, element, context);
            }
            else
            {
                for (int i = 0; i < instance.Length; i++)
                    success |= context.Serializers.Serialize(instance.GetValue(i), element, context);
            }
            return success;
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
                if (element.IsBlittable)
                {
                    var handle = GCHandle.Alloc(instance, GCHandleType.Pinned);
                    var pointer = handle.AddrOfPinnedObject();
                    success |= context.Reader.Read(out ushort size);
                    context.Reader.Read(pointer, instance.Length, size);
                    handle.Free();
                }
                else if (element.Type.IsSealed)
                {
                    var serializer = context.Serializers.Get(element);
                    for (int i = 0; i < instance.Length; i++)
                    {
                        success |= serializer.Instantiate(out var item, element, element, context);
                        if (serializer.Deserialize(ref item, element, element, context)) instance.SetValue(instance, i);
                        else success = false;
                    }
                }
                else
                {
                    for (int i = 0; i < instance.Length; i++)
                    {
                        if (context.Serializers.Deserialize(out var item, element, context)) instance.SetValue(item, i);
                        else success = false;
                    }
                }
            }
            return success;
        }
    }
}