using Entia.Core;
using Entia.Modules.Build;
using Entia.Modules.Control;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Nodes
{
    public static class NodeExtensions
    {
        public static IEnumerable<Node> Family(this Node node) => node.Descendants().Append(node);
        public static IEnumerable<Node> Descendants(this Node node) => node.Children.SelectMany(Family);
        public static Node Descend(this Node node, Func<Node, Node> replace) =>
            replace(node.With(children: node.Children.Select(child => child.Descend(replace)).ToArray()));

        public static Node With(this Node node, string name = null, INode value = null, Node[] children = null) =>
            Node.Of(name ?? node.Name, value ?? node.Value, children ?? node.Children);

        public static Node Wrap<T>(this Node node, in T data) where T : IWrapper => Node.Of(node.Name, data, node);

        public static Node Profile(this Node node, bool recursive = true) => recursive ?
            node.Descend(child => child.Value is IWrapper ? child : child.Wrap(new Profile())) :
            node.Wrap(new Profile());

        public static Node Resolve(this Node node, bool recursive = true)
        {
            if (node.Value is IAtomic) return Node.Of<Resolve>(nameof(Nodes.Resolve), node);
            var children = recursive ? node.Children.Select(child => child.Resolve(recursive)).ToArray() : node.Children;
            return node.With(children: children);
        }
    }
}
