using System;
using System.Reflection;
using System.Runtime.Serialization;
using Entia.Core;
using Entia.Serialization;

namespace Entia.Serializers
{
    public sealed class SerializableObject : Serializer<ISerializable>
    {
        static readonly FormatterConverter _converter = new FormatterConverter();
        static readonly StreamingContext _context = new StreamingContext(StreamingContextStates.All);

        public override bool Serialize(in ISerializable instance, in SerializeContext context)
        {
            var info = new SerializationInfo(context.Type, _converter);
            instance.GetObjectData(info, _context);
            context.Writer.Write(info.MemberCount);
            foreach (var member in info)
            {
                context.Writer.Write(member.Name);
                if (context.Serialize(member.Value)) continue;
                return false;
            }
            return true;
        }

        public override bool Instantiate(out ISerializable instance, in DeserializeContext context)
        {
            var info = new SerializationInfo(context.Type, _converter);
            if (context.Reader.Read(out int count))
            {
                for (int i = 0; i < count; i++)
                {
                    if (context.Reader.Read(out string name) && context.Deserialize(out object value))
                        info.AddValue(name, value);
                }
                var arguments = new object[] { info, _context };
                instance = (ISerializable)Activator.CreateInstance(context.Type, TypeUtility.Instance, null, arguments, null);
                return true;
            }
            instance = default;
            return false;
        }

        public override bool Initialize(ref ISerializable instance, in DeserializeContext context)
        {
            if (instance is IDeserializationCallback callback) callback.OnDeserialization(this);
            return true;
        }
    }
}