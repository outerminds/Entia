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
            var members = new Node[info.MemberCount];
            var index = 0;
            foreach (var pair in info)
                members[index++] = Node.Member(pair.Name, context.Convert(pair.Value));
            return Node.Object(members);
        }

        public override ISerializable Instantiate(in ConvertFromContext context) =>
            FormatterServices.GetUninitializedObject(context.Type) as ISerializable;

        public override void Initialize(ref ISerializable instance, in ConvertFromContext context)
        {
            var info = new SerializationInfo(context.Type, _converter);
            foreach (var member in context.Node.Children)
            {
                if (member.TryMember(out var key, out var value))
                    info.AddValue(key, context.Convert<object>(value));
            }
            context.Type.SerializationConstructor.Invoke(instance, new object[] { info, _context });
            if (instance is IDeserializationCallback callback) callback.OnDeserialization(this);
        }
    }
}