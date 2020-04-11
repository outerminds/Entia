using System;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Schedule;
using Entia.Systems;
using Entia.Scheduling;

namespace Entia.Schedulers
{
    public sealed class Initialize : Scheduler<IInitialize>
    {
        public override Type[] Phases => new[] { typeof(Phases.Initialize) };
        public override Phase[] Schedule(IInitialize instance, in Context context) =>
            new[] { Phase.From<Phases.Initialize>(instance.Initialize) };
    }

    public sealed class Dispose : Scheduler<IDispose>
    {
        public override Type[] Phases => new[] { typeof(Phases.Dispose) };
        public override Phase[] Schedule(IDispose instance, in Context context) =>
            new[] { Phase.From<Phases.Dispose>(instance.Dispose) };
    }

    public sealed class Run : Scheduler<IRun>
    {
        public override Type[] Phases => new[] { typeof(Phases.Run) };
        public override Phase[] Schedule(IRun instance, in Context context) =>
            new[] { Phase.From<Phases.Run>(instance.Run) };
    }

    public sealed class OnMessage<T> : Scheduler<IReact<T>> where T : struct, IMessage
    {
        public override Type[] Phases => React.Phases<T>();

        public override Phase[] Schedule(IReact<T> instance, in Context context)
        {
            var run = new InAction<T>(instance.React);
            return React.Schedule<T>(
                Phase.From((in Phases.React<T> phase) => run(phase.Message)),
                context.Controller,
                context.World);
        }
    }

    public sealed class OnAdd<T> : Scheduler<IOnAdd<T>> where T : struct, IComponent
    {
        public override Type[] Phases => React.Phases<Messages.OnAdd<T>>();

        delegate void Run(Entity entity, ref T component);

        public override Phase[] Schedule(IOnAdd<T> instance, in Context context)
        {
            var components = context.Controller.World.Components();
            var run = new Run(instance.OnAdd);
            return React.Schedule<Messages.OnAdd<T>>(Phase.From((in Phases.React<Messages.OnAdd<T>> phase) =>
            {
                if (components.TryStore<T>(phase.Message.Entity, out var store, out var index))
                    run(phase.Message.Entity, ref store[index]);
            }), context.Controller, context.World);
        }
    }

    public sealed class OnRemove<T> : Scheduler<IOnRemove<T>> where T : struct, IComponent
    {
        public override Type[] Phases => React.Phases<Messages.OnRemove<T>>();

        delegate void Run(Entity entity, ref T component);

        public override Phase[] Schedule(IOnRemove<T> instance, in Context context)
        {
            var components = context.Controller.World.Components();
            var run = new Run(instance.OnRemove);
            return React.Schedule<Messages.OnRemove<T>>(Phase.From((in Phases.React<Messages.OnRemove<T>> phase) =>
            {
                if (components.TryStore<T>(phase.Message.Entity, out var store, out var index))
                    run(phase.Message.Entity, ref store[index]);
            }), context.Controller, context.World);
        }
    }

    public sealed class OnEnable<T> : Scheduler<IOnEnable<T>> where T : struct, IComponent
    {
        public override Type[] Phases => React.Phases<Messages.OnEnable<T>>();

        delegate void Run(Entity entity, ref T component);

        public override Phase[] Schedule(IOnEnable<T> instance, in Context context)
        {
            var components = context.Controller.World.Components();
            var run = new Run(instance.OnEnable);
            return React.Schedule<Messages.OnEnable<T>>(Phase.From((in Phases.React<Messages.OnEnable<T>> phase) =>
            {
                if (components.TryStore<T>(phase.Message.Entity, out var store, out var index))
                    run(phase.Message.Entity, ref store[index]);
            }), context.Controller, context.World);
        }
    }

    public sealed class OnDisable<T> : Scheduler<IOnDisable<T>> where T : struct, IComponent
    {
        public override Type[] Phases => React.Phases<Messages.OnDisable<T>>();

        delegate void Run(Entity entity, ref T component);

        public override Phase[] Schedule(IOnDisable<T> instance, in Context context)
        {
            var components = context.Controller.World.Components();
            var run = new Run(instance.OnDisable);
            return React.Schedule<Messages.OnDisable<T>>(Phase.From((in Phases.React<Messages.OnDisable<T>> phase) =>
            {
                if (components.TryStore<T>(phase.Message.Entity, out var store, out var index))
                    run(phase.Message.Entity, ref store[index]);
            }), context.Controller, context.World);
        }
    }

    public static class React
    {
        public static Type[] Phases<T>() where T : struct, IMessage => new[]
        {
            typeof(Phases.React<T>),
            typeof(Phases.React.Initialize),
            typeof(Phases.React.Dispose),
        };

        public static Phase[] Schedule<T>(Phase phase, Controller controller, World world) where T : struct, IMessage
        {
            var reaction = world.Messages().Reaction<T>();
            var react = new InAction<T>((in T message) => controller.Run(new Phases.React<T> { Message = message }));
            return new[]
            {
                phase,
                Phase.From((in Phases.React.Initialize _) => reaction.Add(react), Phase.Targets.Root, reaction),
                Phase.From((in Phases.React.Dispose _) => reaction.Remove(react), Phase.Targets.Root, reaction)
            };
        }
    }
}
