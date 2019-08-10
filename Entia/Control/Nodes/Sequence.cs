using Entia.Build;
using Entia.Builders;
using Entia.Core;
using Entia.Modules.Schedule;
using Entia.Phases;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Nodes
{
    public readonly struct Sequence : INode, IImplementation<Sequence.Builder>
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
                foreach (var child in Children) if (child.Specialize<T>(controller).TryValue(out var @delegate)) run += @delegate;
                if (run == null) return Option.None();
                return run;
            }
        }

        sealed class Builder : Builder<Sequence>
        {
            public override Result<IRunner> Build(in Sequence data, in Context context)
            {
                var children = context.Node.Children;
                if (children.Length == 1) return context.Build(children[0]);
                return children
                    .Select(context, (child, state) => state.Build(child))
                    .All()
                    .Map(runners => new Runner(runners))
                    .Cast<IRunner>();
            }
        }
    }
}
