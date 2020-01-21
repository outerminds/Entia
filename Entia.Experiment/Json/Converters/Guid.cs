using System;

namespace Entia.Json.Converters
{
    public sealed class ConcreteGuid : Converter<Guid>
    {
        public override Node Convert(in Guid instance, in ConvertToContext context) => instance.ToString();
        public override Guid Instantiate(in ConvertFromContext context) => Guid.Parse(context.Node.Value);
    }
}