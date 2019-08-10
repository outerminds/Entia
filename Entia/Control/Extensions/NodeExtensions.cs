using Entia.Core;
using Entia.Modules.Control;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Nodes
{
    public static class NodeExtensions
    {
        static readonly Lazy<Visitor> _resolve = new Lazy<Visitor>(() =>
        {
            var visitor = new Visitor();
            // NOTE: do not visit children
            visitor.Add<IAtomic>((node, _) => node.Resolve(false));
            return visitor;
        });

        public static IEnumerable<Node> Family(this Node node) => node.Descendants().Append(node);
        public static IEnumerable<Node> Descendants(this Node node) => node.Children.SelectMany(Family);

        public static Node Descend(this Node node, Func<Node, Node> replace) =>
            replace(node.With(children: node.Children.Select(child => child.Descend(replace))));

        public static Node With(this Node node, string name = null, INode value = null, IEnumerable<Node> children = null) =>
            Node.From(name ?? node.Name, value ?? node.Value, children?.ToArray() ?? node.Children);

        public static Node Wrap<T>(this Node node, in T data) where T : IWrapper => Node.From(node.Name, data, node);

        public static Node Profile(this Node node, bool recursive = true) => recursive ?
            node.Descend(child => child.Value is IWrapper ? child : child.Profile(false)) :
            node.Wrap(new Profile());

        public static Node Resolve(this Node node, bool recursive = true) => recursive ?
            _resolve.Value.Visit(node) :
            Node.From<Resolve>(nameof(Nodes.Resolve), node);

        public static Node Flatten(this Node node, bool recursive = true) => recursive ?
            node.Descend(child => child.Flatten(false)) :
            node.Value is Sequence ?
                node.With(children: node.Children.SelectMany(child => child.Value is Sequence ? child.Children : new[] { child })) :
                node;
    }
}
