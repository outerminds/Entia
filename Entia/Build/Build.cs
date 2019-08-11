using Entia.Builders;
using Entia.Core;
using Entia.Nodes;

namespace Entia.Build
{
    public readonly struct Context
    {
        public readonly Node Node;
        public readonly Node Root;
        public readonly World World;

        public Context(Node root, World world) : this(null, root, world) { }
        public Context(Node node, Node root, World world) { Node = node; Root = root; World = world; }

        public Result<IRunner> Build(Node node) => World.Container.Get<IBuilder>(node.Value.GetType())
            .Select(With(node), (builder, state) => builder.Build(state))
            .Choose()
            .FirstOrFailure();
        public Context With(Node node = null) => new Context(node ?? Node, Root, World);
    }

    public static class Extensions
    {
        public static Result<IRunner> Build(this World world, Node node, Node root) =>
            new Context(root, world).Build(node);
    }
}
