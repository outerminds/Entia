using Entia.Builders;
using Entia.Core;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Phases;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Entia.Nodes
{
    public readonly struct Profile : IWrapper
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
                if (Child.Specialize<T>(controller).TryValue(out var child))
                {
                    var messages = controller.World.Messages();
                    var watch = new Stopwatch();
                    return new Runner<T>((in T phase) =>
                    {
                        watch.Restart();
                        child.Run(phase);
                        watch.Stop();
                        messages.Emit(new OnProfile { Runner = this, Phase = typeof(T), Elapsed = watch.Elapsed });
                    });
                }
                return Option.None();
            }
        }

        sealed class Builder : Builder<Runner>
        {
            public override Result<Runner> Build(Node node, Node root, World world) => Result.Cast<Profile>(node.Value)
                .Bind(_ => world.Builders().Build(Node.Sequence(node.Children), root))
                .Map(child => new Runner(child));
        }

        [Builder]
        static readonly Builder _builder = new Builder();
    }
}
