using System;

namespace Entia.Json.Converters
{
    public sealed class ConcreteGuid : Converter<Guid>
    {
        public override Node Convert(in Guid instance, in ToContext context) =>
            instance == Guid.Empty ? "" : instance.ToString();
        public override Guid Instantiate(in FromContext context) =>
            Guid.TryParse(context.Node.AsString(), out var value) ? value : Guid.Empty;
    }
}