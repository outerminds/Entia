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
        public override Phase[] Schedule(IReact<T> instance, Controller controller, World world)
        {
            var run = new InAction<T>(instance.React);
            var messages = world.Messages();
            var reaction = messages.Reaction<T>();
            return new[]
            {
                Phase.From<Phases.React.Initialize>(() =>
                {
                    var runner = controller.Runner<Phases.React<T>>();
                    reaction.Add((in T message) => runner.Run(new Phases.React<T>{ Message = message }));
                }),
                Phase.From<Phases.React.Dispose>(() => messages.Remove(reaction)),
                Phase.From((in Phases.React<T> phase) => run(phase.Message))
            };
        }
    }
}
