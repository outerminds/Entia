using Entia.Builders;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Phases;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Nodes
{
    public readonly struct Automatic : IAtomic
    {
        sealed class Builder : IBuilder
        {
            public Result<IRunner> Build(Node node, Node root, World world)
            {
                if (node.Children.Length <= 1) return world.Builders().Build(Node.Sequence(node.Children), root);

                var sets = new[] { node.Children, node.Children.Reverse() }
                    .SelectMany(runners => runners
                        .Select(runner => runners
                            .Except(runner)
                            .Aggregate(
                                new[] { runner },
                                (group, current) =>
                                {
                                    var nodes = group.Append(current).ToArray();
                                    var parallel = Node.Parallel(nodes);
                                    var result = world.Analyzers().Analyze(parallel, root);
                                    return result.IsSuccess() ? nodes : group;
                                })
                            .ToSet()))
                    .ToArray();

                var groups = new List<Node>();
                var remaining = new HashSet<Node>(node.Children);
                while (remaining.Count > 0)
                {
                    var set = sets
                        .Where(group => group.Count > 0)
                        .OrderByDescending(group => group.Count)
                        .Select(group => group.ToArray())
                        .FirstOrDefault() ?? Array.Empty<Node>();

                    sets.Iterate(group => group.ExceptWith(set));
                    remaining.ExceptWith(set);
                    groups.Add(Node.Parallel(set));
                }

                return world.Builders().Build(Node.Sequence(node.Name, groups.ToArray()), root);
            }
        }

        [Builder]
        static readonly Builder _builder = new Builder();
    }
}
