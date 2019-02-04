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
            replace(Node.Of(node.Name, node.Value, node.Children.Select(child => child.Descend(replace)).ToArray()));

        public static Node Profile(this Node node, bool recursive = true) => recursive ?
            node.Descend(child => child.Value is IWrapper ? child : child.Profile(false)) :
            node.Wrap(new Profile());

        public static Node Flatten(this Node node, bool recursive = true)
        {
            if (node.Value is IAtomic) return node;
            var children = recursive ? node.Children.Select(child => child.Flatten(recursive)).ToArray() : node.Children;
            return Node.Of(node.Name, node.Value, children
                .SelectMany(child => child.Value is IAtomic || child.Children.Length == 0 ? new[] { child } : child.Children)
                .ToArray());
        }

        public static Node Wrap<T>(this Node node, in T data) where T : IWrapper => Node.Of(node.Name, data, node);
        public static Node Repeat(this Node node, int count) => Node.Of(node.Name, node.Value, node.Children.Repeat(count).ToArray());
        public static Node Distinct(this Node node) => Node.Of(node.Name, node.Value, node.Children.Distinct().ToArray());
        public static Node Separate(this Node node, Node separator, bool recursive = true) => node.Separate(() => separator, recursive);
        public static Node Separate(this Node node, Func<Node> provider, bool recursive = true)
        {
            if (node.Value is IAtomic) return node;
            var children = recursive ? node.Children.Select(child => child.Separate(provider, recursive)).ToArray() : node.Children;
            return Node.Of(node.Name, node.Value, children.Separate(provider).ToArray());
        }

        public static Node Resolve(this Node node)
        {
            var children = node.Children.Select(child => child.Value is IAtomic ? Node.Resolve(child) : child.Resolve()).ToArray();
            return Node.Of(node.Name, node.Value, children);
        }
    }
}
