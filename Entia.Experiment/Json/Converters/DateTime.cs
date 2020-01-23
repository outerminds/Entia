using System;

namespace Entia.Json.Converters
{
    public sealed class ConcreteDateTime : Converter<DateTime>
    {
        public override Node Convert(in DateTime instance, in ConvertToContext context) =>
            Node.Array(instance.Ticks, (int)instance.Kind);
        public override DateTime Instantiate(in ConvertFromContext context) =>
            context.Node.IsArray() ? new DateTime(context.Node.AsLong(0), (DateTimeKind)context.Node.AsInt(1)) :
            DateTime.Parse(context.Node.AsString());
    }
}