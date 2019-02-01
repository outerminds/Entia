using System;
using System.Collections.Generic;
using System.Linq;
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
        public override IEnumerable<Phase> Schedule(IInitialize instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.Initialize>(instance.Initialize) };
    }

    public sealed class Dispose : Scheduler<IDispose>
    {
        public override IEnumerable<Phase> Schedule(IDispose instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.Dispose>(instance.Dispose) };
    }

    public sealed class Run : Scheduler<IRun>
    {
        public override IEnumerable<Phase> Schedule(IRun instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.Run>(instance.Run) };
    }

    public sealed class React<T> : Scheduler<IReact<T>> where T : struct, IMessage
    {
        public override IEnumerable<Phase> Schedule(IReact<T> instance, Controller controller, World world)
        {
            var run = new InAction<T>(instance.React);
            yield return Phase.From((in Phases.React<T> phase) => run(phase.Message));

            var messages = world.Messages();
            var reaction = messages.Reaction<T>();
            var box = controller.Box<Phases.React<T>>();
            var react = new InAction<T>((in T message) => box.Value.Run(new Phases.React<T> { Message = message }));
            yield return Phase.From((in Phases.React.Initialize _) => reaction.Add(react));
            yield return Phase.From((in Phases.React.Dispose _) => reaction.Remove(react));
        }
    }
}
