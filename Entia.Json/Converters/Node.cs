namespace Entia.Json.Converters
{
    public sealed class ConcreteNode : Converter<Node>
    {
        public override Node Convert(in Node instance, in ToContext context) => instance;
        public override Node Instantiate(in FromContext context) => context.Node;
    }
}