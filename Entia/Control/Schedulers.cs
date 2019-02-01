using System;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Systems;

namespace Entia.Schedulers
{
    public sealed class Initialize : Scheduler<IInitialize>
    {
        public override Phase[] Schedule(IInitialize instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.Initialize>(instance.Initialize) };
    }

    public sealed class Dispose : Scheduler<IDispose>
    {
        public override Phase[] Schedule(IDispose instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.Dispose>(instance.Dispose) };
    }

    public sealed class Run : Scheduler<IRun>
    {
        public override Phase[] Schedule(IRun instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.Run>(instance.Run) };
    }

    public sealed class React<T> : Scheduler<IReact<T>> where T : struct, IMessage
    {
        struct DoRun<TPhase> : IMessage where TPhase : struct, Phases.IPhase { public TPhase Phase; }

        public override Phase[] Schedule(IReact<T> instance, Controller controller, World world)
        {
            var run = new InAction<T>(instance.React);
            var react = Phase.From((in Phases.React<T> phase) => run(phase.Message));
            var messages = world.Messages();
            var reaction = messages.Reaction<T>();

            // NOTE: this check prevents multiple calls to 'Runner<Phases.React<T>>.Run' since only 1 is required.
            if (messages.Has<DoRun<Phases.React<T>>>()) return new[] { react };

            var doRun = messages.Reaction<DoRun<Phases.React<T>>>();
            // NOTE: get the runner after getting the 'doRun' reaction to prevent infinite loop.
            var runner = controller.Runner<Phases.React<T>>();
            doRun.Add((in DoRun<Phases.React<T>> message) => runner.Run(message.Phase));
            return new[]
            {
                Phase.From((in Phases.React.Initialize _) => reaction.Add((in T message) =>
                    doRun.React( new DoRun<Phases.React<T>> { Phase = new Phases.React<T> { Message = message } }))),
                Phase.From((in Phases.React.Dispose _) => doRun.Clear()),
                react
            };
        }
    }
}
