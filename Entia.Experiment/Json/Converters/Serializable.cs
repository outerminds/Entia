using System.Reflection;
using System.Runtime.Serialization;
using Entia.Core;

namespace Entia.Json.Converters
{
    public sealed class AbstractSerializable : Converter<ISerializable>
    {
        static readonly FormatterConverter _converter = new FormatterConverter();
        static readonly StreamingContext _context = new StreamingContext(StreamingContextStates.All);

        public override bool CanConvert(TypeData type) => type.SerializationConstructor is ConstructorInfo;

        public override Node Convert(in ISerializable instance, in ConvertToContext context)
        {
            var info = new SerializationInfo(context.Type, _converter);
            instance.GetObjectData(info, _context);
            var children = new Node[info.MemberCount * 2];
            var index = 0;
            foreach (var pair in info)
            {
                children[index++] = pair.Name;
                children[index++] = context.Convert(pair.Value);
            }
            return Node.Object(children);
        }

        public override ISerializable Instantiate(in ConvertFromContext context) =>
            FormatterServices.GetUninitializedObject(context.Type) as ISerializable;

        public override void Initialize(ref ISerializable instance, in ConvertFromContext context)
        {
            var info = new SerializationInfo(context.Type, _converter);
            var children = context.Node.Children;
            for (int i = 1; i < children.Length; i += 2)
            {
                if (children[i - 1].TryString(out var key))
                    info.AddValue(key, context.Convert<object>(children[i]));
            }
            context.Type.SerializationConstructor.Invoke(instance, new object[] { info, _context });
            if (instance is IDeserializationCallback callback) callback.OnDeserialization(this);
        }
    }
}