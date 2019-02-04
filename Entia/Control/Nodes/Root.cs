using System;
using System.Collections.Generic;
using System.Linq;
using Entia.Builders;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Phases;

namespace Entia.Nodes
{
    public readonly struct Root : IWrapper
    {
        sealed class Runner : IRunner
        {
            public object Instance => Child;
            public readonly IRunner Child;
            public Runner(IRunner child) { Child = child; }

            public IEnumerable<Type> Phases() => Child.Phases();
            public IEnumerable<Phase> Phases(Controller controller) => Child.Phases(controller);
            public Option<Runner<T>> Specialize<T>(Controller controller) where T : struct, IPhase
            {
                var runs = Child.Phases(controller)
                    .Where(phase => phase.Target == Phase.Targets.Root && phase.Type == typeof(T))
                    .DistinctBy(phase => phase.Distinct)
                    .Select(phase => phase.Delegate)
                    .OfType<InAction<T>>();
                if (Child.Specialize<T>(controller).TryValue(out var child))
                    return new Runner<T>(runs.Aggregate(child.Run, (sum, current) => sum + current));
                else
                {
                    var run = runs.Aggregate(default(InAction<T>), (sum, current) => sum + current);
                    if (run == null) return Option.None();
                    return new Runner<T>(run);
                }
            }
        }

        sealed class Builder : Builder<Runner>
        {
            public override Result<Runner> Build(Node node, Node root, World world) => Result.Cast<Root>(node.Value)
                .Bind(_ => world.Builders().Build(Node.Sequence(node.Name, node.Children), root))
                .Map(child => new Runner(child));
        }

        [Builder]
        static readonly Builder _builder = new Builder();
    }
}