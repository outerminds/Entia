using Entia.Core;
using Entia.Dependencies;
using Entia.Nodes;

namespace Entia.Analysis
{
    public readonly struct Context
    {
        public readonly Node Node;
        public readonly Node Root;
        public readonly World World;

        public Context(Node root, World world) : this(null, root, world) { }
        public Context(Node node, Node root, World world) { Node = node; Root = root; World = world; }

        public Result<IDependency[]> Analyze(Node node) => World.Container.Get<IAnalyzer>(node.Value.GetType())
            .Select(With(node), (analyzer, state) => analyzer.Analyze(state))
            .FirstOrFailure()
            .Flatten();
        public Context With(Node node = null) => new Context(node ?? Node, Root, World);
    }

    public static class Extensions
    {
        public static Result<IDependency[]> Analyze(this World world, Node node, Node root) =>
            new Context(root, world).Analyze(node);
    }
}
