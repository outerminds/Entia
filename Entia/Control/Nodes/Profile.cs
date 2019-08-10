using Entia.Build;
using Entia.Builders;
using Entia.Core;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Schedule;
using Entia.Phases;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Entia.Nodes
{
    public readonly struct Profile : IWrapper, IImplementation<Profile.Builder>
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
                if (Child.Specialize<T>(controller).TryValue(out var child))
                {
                    var messages = controller.World.Messages();
                    var onProfile = messages.Emitter<OnProfile>();
                    var watch = new Stopwatch();
                    void Run(in T phase)
                    {
                        watch.Restart();
                        child(phase);
                        watch.Stop();
                        onProfile.Emit(new OnProfile { Runner = this, Phase = typeof(T), Elapsed = watch.Elapsed });
                    }
                    return new Run<T>(Run);
                }
                return Option.None();
            }
        }

        sealed class Builder : Builder<Profile>
        {
            public override Result<IRunner> Build(in Profile data, in Context context) =>
                context.Build(Node.Sequence(context.Node.Children)).Map(child => new Runner(child)).Cast<IRunner>();
        }
    }
}
