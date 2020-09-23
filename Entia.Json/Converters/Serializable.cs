using System.Reflection;
using System.Runtime.Serialization;

namespace Entia.Json.Converters
{
    public sealed class AbstractSerializable : Converter<ISerializable>
    {
        static readonly FormatterConverter _formatter = new FormatterConverter();
        static readonly StreamingContext _context = new StreamingContext(StreamingContextStates.All);

        readonly ConstructorInfo _constructor;

        public AbstractSerializable(ConstructorInfo constructor) { _constructor = constructor; }

        public override Node Convert(in ISerializable instance, in ToContext context)
        {
            var info = new SerializationInfo(context.Type, _formatter);
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

        public override ISerializable Instantiate(in FromContext context) =>
            FormatterServices.GetUninitializedObject(context.Type) as ISerializable;

        public override void Initialize(ref ISerializable instance, in FromContext context)
        {
            var info = new SerializationInfo(context.Type, _formatter);
            var children = context.Node.Children;
            for (int i = 1; i < children.Length; i += 2)
            {
                if (children[i - 1].TryString(out var key))
                    info.AddValue(key, context.Convert<object>(children[i]));
            }
            _constructor.Invoke(instance, new object[] { info, _context });
            if (instance is IDeserializationCallback callback) callback.OnDeserialization(this);
        }
    }
}