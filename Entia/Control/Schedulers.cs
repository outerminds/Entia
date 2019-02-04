using System;
using System.Collections.Generic;
using System.Linq;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Nodes;
using Entia.Phases;
using Entia.Systems;

namespace Entia.Schedulers
{
    public sealed class Initialize : Scheduler<IInitialize>
    {
        public override IEnumerable<Type> Phases => new[] { typeof(Phases.Initialize) };
        public override IEnumerable<Phase> Schedule(IInitialize instance, Controller controller) =>
            new[] { Phase.From<Phases.Initialize>(instance.Initialize) };
    }

    public sealed class Dispose : Scheduler<IDispose>
    {
        public override IEnumerable<Type> Phases => new[] { typeof(Phases.Dispose) };
        public override IEnumerable<Phase> Schedule(IDispose instance, Controller controller) =>
            new[] { Phase.From<Phases.Dispose>(instance.Dispose) };
    }

    public sealed class Run : Scheduler<IRun>
    {
        public override IEnumerable<Type> Phases => new[] { typeof(Phases.Run) };
        public override IEnumerable<Phase> Schedule(IRun instance, Controller controller) =>
            new[] { Phase.From<Phases.Run>(instance.Run) };
    }

    public sealed class React<T> : Scheduler<IReact<T>> where T : struct, IMessage
    {
        public override IEnumerable<Type> Phases => new[]
        {
            typeof(Phases.React<T>),
            typeof(Phases.React.Initialize),
            typeof(Phases.React.Dispose),
        };

        public override IEnumerable<Phase> Schedule(IReact<T> instance, Controller controller)
        {
            var run = new InAction<T>(instance.React);
            yield return Phase.From((in Phases.React<T> phase) => run(phase.Message));

            var reaction = controller.World.Messages().Reaction<T>();
            var react = new InAction<T>((in T message) => controller.Run(new Phases.React<T> { Message = message }));
            yield return Phase.From((in Phases.React.Initialize _) => reaction.Add(react), Phase.Targets.Root, GetType());
            yield return Phase.From((in Phases.React.Dispose _) => reaction.Remove(react), Phase.Targets.Root, GetType());
        }
    }
}
