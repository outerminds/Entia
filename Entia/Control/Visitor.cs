using System;
using System.Linq;
using Entia.Core;
using Entia.Nodes;

namespace Entia.Modules.Control
{
    public sealed class Visitor
    {
        readonly TypeMap<INode, Func<Node, Node>> _visits = new TypeMap<INode, Func<Node, Node>>();
        readonly Func<Node, Node> _default;

        public Visitor(Func<Node, Node> @default = null)
        {
            _default = @default ?? (node => node.With(children: node.Children.Select(Visit).ToArray()));
        }

        public Node Visit(Node node) =>
            node.Value is INode value && TryGet(value.GetType(), out var visit) ? visit(node) : _default(node);
        public bool TryGet(Type type, out Func<Node, Node> visit) =>
            _visits.TryGet(type, out visit, super: true);
        public bool Add<T>(Func<Node, T, Node> visit) where T : INode =>
            _visits.Set<T>(node => node.Value is T casted ? visit(node, casted) : node);
        public bool Add<T>(Action<Node, T> visit) where T : INode =>
            Add<T>((node, value) => { visit(node, value); return node; });

        public bool Remove<T>() where T : struct, INode => _visits.Remove<T>();
        public bool Remove(Type type) => _visits.Remove(type);
        public bool Clear() => _visits.Clear();
    }
}