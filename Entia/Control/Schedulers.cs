using System;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Systems;

namespace Entia.Schedulers
{
    public sealed class PreInitialize : Scheduler<IPreInitialize>
    {
        public override Phase[] Schedule(IPreInitialize instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.PreInitialize>(instance.PreInitialize) };
    }

    public sealed class PostInitialize : Scheduler<IPostInitialize>
    {
        public override Phase[] Schedule(IPostInitialize instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.PostInitialize>(instance.PostInitialize) };
    }

    public sealed class Initialize : Scheduler<IInitialize>
    {
        public override Phase[] Schedule(IInitialize instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.Initialize>(instance.Initialize) };
    }

    public sealed class PreDispose : Scheduler<IPreDispose>
    {
        public override Phase[] Schedule(IPreDispose instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.PreDispose>(instance.PreDispose) };
    }

    public sealed class PostDispose : Scheduler<IPostDispose>
    {
        public override Phase[] Schedule(IPostDispose instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.PostDispose>(instance.PostDispose) };
    }

    public sealed class Dispose : Scheduler<IDispose>
    {
        public override Phase[] Schedule(IDispose instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.Dispose>(instance.Dispose) };
    }

    public sealed class PreRun : Scheduler<IPreRun>
    {
        public override Phase[] Schedule(IPreRun instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.PreRun>(instance.PreRun) };
    }

    public sealed class PostRun : Scheduler<IPostRun>
    {
        public override Phase[] Schedule(IPostRun instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.PostRun>(instance.PostRun) };
    }

    public sealed class Run : Scheduler<IRun>
    {
        public override Phase[] Schedule(IRun instance, Controller controller, World world) =>
            new[] { Phase.From<Phases.Run>(instance.Run) };
    }

    public sealed class PreReact<T> : Scheduler<IPreReact<T>> where T : struct, IMessage
    {
        public override Phase[] Schedule(IPreReact<T> instance, Controller controller, World world)
        {
            var run = new InAction<T>(instance.PreReact);
            var messages = world.Messages();
            var reaction = messages.Reaction<T>();
            return new[]
            {
                Phase.From<Phases.React.Initialize>(() =>
                {
                    var runner = controller.Runner<Phases.PreReact<T>>();
                    reaction.Add((in T message) => runner.Run(new Phases.PreReact<T>{ Message = message }));
                }),
                Phase.From<Phases.React.Dispose>(() => messages.Remove(reaction)),
                Phase.From((in Phases.PreReact<T> phase) => run(phase.Message))
            };
        }
    }

    public sealed class PostReact<T> : Scheduler<IPostReact<T>> where T : struct, IMessage
    {
        public override Phase[] Schedule(IPostReact<T> instance, Controller controller, World world)
        {
            var run = new InAction<T>(instance.PostReact);
            var messages = world.Messages();
            var reaction = messages.Reaction<T>();
            return new[]
            {
                Phase.From<Phases.React.Initialize>(() =>
                {
                    var runner = controller.Runner<Phases.PostReact<T>>();
                    reaction.Add((in T message) => runner.Run(new Phases.PostReact<T>{ Message = message }));
                }),
                Phase.From<Phases.React.Dispose>(() => messages.Remove(reaction)),
                Phase.From((in Phases.PostReact<T> phase) => run(phase.Message))
            };
        }
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
