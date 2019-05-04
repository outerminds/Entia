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
    public readonly struct Sequence : INode, IBuildable<Sequence.Builder>
    {
        sealed class Runner : IRunner
        {
            public object Instance => Children;
            public readonly IRunner[] Children;
            public Runner(params IRunner[] children) { Children = children; }

            public IEnumerable<Type> Phases() => Children.SelectMany(child => child.Phases());
            public IEnumerable<Phase> Phases(Controller controller) => Children.SelectMany(child => child.Phases(controller));
            public Option<Run<T>> Specialize<T>(Controller controller) where T : struct, IPhase
            {
                var run = default(Run<T>);
                foreach (var child in Children) if (child.Specialize<T>(controller).TryValue(out var special)) run += special;
                if (run == null) return Option.None();
                return run;
            }
        }

        sealed class Builder : IBuilder
        {
            public Result<IRunner> Build(Node node, Node root, World world) => node.Children.Length == 1 ?
                Result.Cast<Sequence>(node.Value).Bind(_ => world.Builders().Build(node.Children[0], root)) :
                Result.Cast<Sequence>(node.Value)
                    .Bind(_ => node.Children.Select(child => world.Builders().Build(child, root)).All())
                    .Map(children => new Runner(children))
                    .Cast<IRunner>();
        }
    }
}
