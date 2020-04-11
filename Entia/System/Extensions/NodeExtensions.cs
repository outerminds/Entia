using System;
using Entia.Core;
using System.Linq;
using Entia.Dependencies;
using System.Collections.Generic;
using Entia.Experimental.Nodes;

namespace Entia.Experimental
{
    public static class NodeExtensions
    {
        public static IEnumerable<Node> Descendants(in this Node node) =>
            node.Children.Length == 0 ? Array.Empty<Node>() :
            node.Children.SelectMany(child => child.Descendants().Prepend(child));

        public static Node Descend(in this Node node, Func<Node, Node> replace) =>
            replace(node.With(node.Children.Select(child => child.Descend(replace))));

        public static Result<Node> Descend(in this Node node, Func<Node, Result<Node>> replace) =>
            node.Children.Select(child => child.Descend(replace)).All().Map(node.With).Bind(replace);

        public static Result<Node> Resolve(in this Node node, World world) =>
            node.Descend(child => child.Data is Lazy data ? data.Provide(world).Bind(value => value.Resolve(world)) : child);

        public static Node Wrap(in this Node node, IWrapper data) => Node.From(data, node);
        public static Node Depend(in this Node node, params IDependency[] dependencies) => node.Wrap(new Depend(dependencies));
    }
}