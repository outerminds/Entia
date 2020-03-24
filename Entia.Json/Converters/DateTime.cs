using System;

namespace Entia.Json.Converters
{
    public sealed class ConcreteDateTime : Converter<DateTime>
    {
        public override Node Convert(in DateTime instance, in ConvertToContext context) =>
            instance == DateTime.MinValue ? -1 :
            instance == DateTime.MaxValue ? 1 :
            Node.Array(instance.Ticks, (int)instance.Kind);
        public override DateTime Instantiate(in ConvertFromContext context) =>
            context.Node.TryInt(out var special) ? special < 0 ? DateTime.MinValue : DateTime.MaxValue :
            context.Node.TryLong(0, out var ticks) && context.Node.TryInt(1, out var kind) ? new DateTime(ticks, (DateTimeKind)kind) :
            context.Node.TryString(out var value) ? DateTime.Parse(value) :
            default;
    }
}