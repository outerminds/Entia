namespace Entia.Json.Converters
{
    public sealed class ConcreteNode : Converter<Node>
    {
        public override Node Convert(in Node instance, in ConvertToContext context) => instance;
        public override Node Instantiate(in ConvertFromContext context) => context.Node;
    }
}