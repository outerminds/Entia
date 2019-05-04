using Entia.Builders;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Phases;
using System;
using System.Collections.Generic;

namespace Entia.Nodes
{
    public readonly struct State : IWrapper, IBuildable<State.Builder>
    {
        sealed class Runner : IRunner
        {
            public object Instance => Child;
            public readonly Func<Controller.States> Get;
            public readonly IRunner Child;
            public Runner(Func<Controller.States> get, IRunner child) { Get = get; Child = child; }

            public IEnumerable<Type> Phases() => Child.Phases();
            public IEnumerable<Phase> Phases(Controller controller) => Child.Phases(controller);
            public Option<Run<T>> Specialize<T>(Controller controller) where T : struct, IPhase
            {
                if (Child.Specialize<T>(controller).TryValue(out var child))
                {
                    void Run(in T phase) { if (Get() == Controller.States.Enabled) child(phase); }
                    return new Run<T>(Run);
                }
                return Option.None();
            }
        }

        sealed class Builder : Builder<Runner>
        {
            public override Result<Runner> Build(Node node, Node root, World world) => Result.Cast<State>(node.Value)
                .Bind(data => world.Builders().Build(Node.Sequence(node.Name, node.Children), root)
                    .Map(child => new Runner(data.Get, child)));
        }

        public readonly Func<Controller.States> Get;
        public State(Func<Controller.States> get) { Get = get; }
    }
}
