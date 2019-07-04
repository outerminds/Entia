using System;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Schedule;
using Entia.Systems;

namespace Entia.Schedulers
{
    public sealed class Initialize : Scheduler<IInitialize>
    {
        public override Type[] Phases => new[] { typeof(Phases.Initialize) };
        public override Phase[] Schedule(IInitialize instance, Controller controller) =>
            new[] { Phase.From<Phases.Initialize>(instance.Initialize) };
    }

    public sealed class Dispose : Scheduler<IDispose>
    {
        public override Type[] Phases => new[] { typeof(Phases.Dispose) };
        public override Phase[] Schedule(IDispose instance, Controller controller) =>
            new[] { Phase.From<Phases.Dispose>(instance.Dispose) };
    }

    public sealed class Run : Scheduler<IRun>
    {
        public override Type[] Phases => new[] { typeof(Phases.Run) };
        public override Phase[] Schedule(IRun instance, Controller controller) =>
            new[] { Phase.From<Phases.Run>(instance.Run) };
    }

    public sealed class React<T> : Scheduler<IReact<T>> where T : struct, IMessage
    {
        public override Type[] Phases => new[]
        {
            typeof(Phases.React<T>),
            typeof(Phases.React.Initialize),
            typeof(Phases.React.Dispose),
        };

        public override Phase[] Schedule(IReact<T> instance, Controller controller)
        {
            var run = new InAction<T>(instance.React);
            var reaction = controller.World.Messages().Reaction<T>();
            var react = new InAction<T>((in T message) => controller.Run(new Phases.React<T> { Message = message }));
            return new[]
            {
                Phase.From((in Phases.React<T> phase) => run(phase.Message)),
                Phase.From((in Phases.React.Initialize _) => reaction.Add(react), Phase.Targets.Root, GetType()),
                Phase.From((in Phases.React.Dispose _) => reaction.Remove(react), Phase.Targets.Root, GetType())
            };
        }
    }
}
