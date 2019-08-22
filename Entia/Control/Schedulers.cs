using System;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Schedule;
using Entia.Systems;
using Entia.Schedule;

namespace Entia.Schedulers
{
    public sealed class Initialize : Scheduler<IInitialize>
    {
        public override Type[] Phases => new[] { typeof(Phases.Initialize) };
        public override Phase[] Schedule(in IInitialize instance, in Context context) =>
            new[] { Phase.From<Phases.Initialize>(instance.Initialize) };
    }

    public sealed class Dispose : Scheduler<IDispose>
    {
        public override Type[] Phases => new[] { typeof(Phases.Dispose) };
        public override Phase[] Schedule(in IDispose instance, in Context context) =>
            new[] { Phase.From<Phases.Dispose>(instance.Dispose) };
    }

    public sealed class Run : Scheduler<IRun>
    {
        public override Type[] Phases => new[] { typeof(Phases.Run) };
        public override Phase[] Schedule(in IRun instance, in Context context) =>
            new[] { Phase.From<Phases.Run>(instance.Run) };
    }

    public abstract class React<T, TMessage> : Scheduler<T> where TMessage : struct, IMessage
    {
        public override Type[] Phases => new[]
        {
            typeof(Phases.React<TMessage>),
            typeof(Phases.React.Initialize),
            typeof(Phases.React.Dispose),
        };

        public override Phase[] Schedule(in T instance, in Context context)
        {
            var controller = context.Controller;
            var reaction = context.World.Messages().Reaction<TMessage>();
            var react = new InAction<TMessage>((in TMessage message) => controller.Run(new Phases.React<TMessage> { Message = message }));
            return new[]
            {
                Run(instance, context),
                Phase.From((in Phases.React.Initialize _) => reaction.Add(react), Phase.Targets.Root, reaction),
                Phase.From((in Phases.React.Dispose _) => reaction.Remove(react), Phase.Targets.Root, reaction)
            };
        }

        public abstract Phase Run(in T instance, in Context context);
    }

    public sealed class OnMessage<T> : React<IReact<T>, T> where T : struct, IMessage
    {
        public override Phase Run(in IReact<T> instance, in Context context)
        {
            var run = new InAction<T>(instance.React);
            return Phase.From((in Phases.React<T> phase) => run(phase.Message));
        }
    }

    public sealed class OnAdd<T> : React<IOnAdd<T>, Messages.OnAdd<T>> where T : struct, IComponent
    {
        delegate void Runner(Entity entity, ref T component);

        public override Phase Run(in IOnAdd<T> instance, in Context context)
        {
            var components = context.Controller.World.Components();
            var run = new Runner(instance.OnAdd);
            return Phase.From((in Phases.React<Messages.OnAdd<T>> phase) =>
            {
                if (components.TryStore<T>(phase.Message.Entity, out var store, out var index))
                    run(phase.Message.Entity, ref store[index]);
            });
        }
    }

    public sealed class OnRemove<T> : React<IOnRemove<T>, Messages.OnRemove<T>> where T : struct, IComponent
    {
        delegate void Runner(Entity entity, ref T component);

        public override Phase Run(in IOnRemove<T> instance, in Context context)
        {
            var components = context.Controller.World.Components();
            var run = new Runner(instance.OnRemove);
            return Phase.From((in Phases.React<Messages.OnRemove<T>> phase) =>
            {
                if (components.TryStore<T>(phase.Message.Entity, out var store, out var index))
                    run(phase.Message.Entity, ref store[index]);
            });
        }
    }

    public sealed class OnEnable<T> : React<IOnEnable<T>, Messages.OnEnable<T>> where T : struct, IComponent
    {
        delegate void Runner(Entity entity, ref T component);

        public override Phase Run(in IOnEnable<T> instance, in Context context)
        {
            var components = context.Controller.World.Components();
            var run = new Runner(instance.OnEnable);
            return Phase.From((in Phases.React<Messages.OnEnable<T>> phase) =>
            {
                if (components.TryStore<T>(phase.Message.Entity, out var store, out var index))
                    run(phase.Message.Entity, ref store[index]);
            });
        }
    }

    public sealed class OnDisable<T> : React<IOnDisable<T>, Messages.OnDisable<T>> where T : struct, IComponent
    {
        delegate void Runner(Entity entity, ref T component);

        public override Phase Run(in IOnDisable<T> instance, in Context context)
        {
            var components = context.Controller.World.Components();
            var run = new Runner(instance.OnDisable);
            return Phase.From((in Phases.React<Messages.OnDisable<T>> phase) =>
            {
                if (components.TryStore<T>(phase.Message.Entity, out var store, out var index))
                    run(phase.Message.Entity, ref store[index]);
            });
        }
    }
}
