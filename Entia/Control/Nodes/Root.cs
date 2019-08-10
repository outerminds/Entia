using System;
using System.Collections.Generic;
using Entia.Build;
using Entia.Builders;
using Entia.Core;
using Entia.Modules.Schedule;
using Entia.Phases;

namespace Entia.Nodes
{
    public readonly struct Root : IWrapper, IImplementation<Root.Builder>
    {
        sealed class Runner : IRunner
        {
            public object Instance => Child;
            public readonly IRunner Child;
            public Runner(IRunner child) { Child = child; }

            public IEnumerable<Type> Phases() => Child.Phases();
            public IEnumerable<Phase> Phases(Controller controller) => Child.Phases(controller);
            public Option<Run<T>> Specialize<T>(Controller controller) where T : struct, IPhase
            {
                var run = Child.Specialize<T>(controller).TryValue(out var child) ? child : default;
                var set = new HashSet<object>();
                foreach (var phase in Phases(controller))
                {
                    if (phase.Target == Phase.Targets.Root && phase.Type == typeof(T) && set.Add(phase.Distinct) && phase.Delegate is Run<T> @delegate)
                        run += @delegate;
                }
                if (run == null) return Option.None();
                return run;
            }
        }

        sealed class Builder : Builder<Root>
        {
            public override Result<IRunner> Build(in Root data, in Context context) =>
                context.Build(Node.Sequence(context.Node.Name, context.Node.Children)).Map(child => new Runner(child)).Cast<IRunner>();
        }
    }
}