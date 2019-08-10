using Entia.Analysis;
using Entia.Build;
using Entia.Builders;
using Entia.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Nodes
{
    public readonly struct Automatic : IAtomic, IImplementation<Automatic.Builder>
    {
        sealed class Builder : Builder<Automatic>
        {
            public override Result<IRunner> Build(in Automatic data, in Build.Context context)
            {
                var world = context.World;
                var root = context.Root;
                var children = context.Node.Children;
                if (children.Length <= 1) return context.Build(Node.Sequence(children));

                var sets = new[] { children, children.Reverse() }
                    .SelectMany(runners => runners
                        .Select(runner => runners
                            .Except(runner)
                            .Aggregate(
                                new[] { runner },
                                (group, current) =>
                                {
                                    var nodes = group.Append(current).ToArray();
                                    var parallel = Node.Parallel(nodes);
                                    var result = world.Analyze(parallel, root);
                                    return result.IsSuccess() ? nodes : group;
                                })
                            .ToSet()))
                    .ToArray();

                var groups = new List<Node>();
                var remaining = new HashSet<Node>(children);
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

                return context.Build(Node.Sequence(context.Node.Name, groups.ToArray()));
            }
        }
    }
}
