using Entia.Build;
using Entia.Builders;
using Entia.Core;
using Entia.Modules.Schedule;
using Entia.Phases;
using System;
using System.Collections.Generic;

namespace Entia.Nodes
{
    public readonly struct Resolve : IWrapper, IImplementation<Resolve.Builder>
    {
        sealed class Runner : IRunner
        {
            public object Instance => Child;
            public readonly IRunner Child;
            public Runner(IRunner child) { Child = child; }

            public IEnumerable<Type> Phases() => Child.Phases();
            public IEnumerable<Phase> Schedule(Controller controller) => Child.Schedule(controller);
            public Option<Run<T>> Specialize<T>(Controller controller) where T : struct, IPhase
            {
                if (Child.Specialize<T>(controller).TryValue(out var child))
                {
                    if (typeof(T).Is<IResolve>())
                    {
                        void Run(in T phase) { child(phase); controller.World.Resolve(); }
                        return new Run<T>(Run);
                    }
                    return child;
                }
                return Option.None();
            }
        }

        sealed class Builder : Builder<Resolve>
        {
            public override Result<IRunner> Build(in Resolve data, in Context context) =>
                context.Build(Node.Sequence(context.Node.Name, context.Node.Children)).Map(child => new Runner(child)).Cast<IRunner>();
        }
    }
}
