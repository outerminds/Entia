using System;
using Entia.Core;
using System.Linq;
using Entia.Dependencies;
using System.Collections.Generic;

namespace Entia.Experimental.Nodes
{
    public static class NodeExtensions
    {
        public static IEnumerable<Node> Descendants(this Node node) =>
            node.Children.Length == 0 ? Array.Empty<Node>() :
            node.Children.SelectMany(child => child.Descendants().Prepend(child));

        public static Node Descend(this Node node, Func<Node, Node> replace) =>
            replace(node.With(node.Children.Select(child => child.Descend(replace))));

        public static Result<Node> Descend(this Node node, Func<Node, Result<Node>> replace) =>
            node.Children.Select(child => child.Descend(replace)).All().Map(node.With).Bind(replace);

        public static Result<Node> Expand(this Node node, World world) =>
            node.Descend(child => child.Value is Lazy data ? data.Provide(world).Bind(value => value.Expand(world)) : child);

        public static Node Resolve(this Node node) =>
            node.Value is IAtomic ? node.Wrap(new Resolve()) : node.With(node.Children.Select(Resolve));

        public static Node Wrap(this Node node, IWrapper data) => Node.From(data, node);
        public static Node Depend(this Node node, params IDependency[] dependencies) => node.Wrap(new Depend(dependencies));
    }
}