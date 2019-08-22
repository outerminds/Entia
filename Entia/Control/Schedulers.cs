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
            var run = Run(instance, context);
            var reaction = context.World.Messages().Reaction<TMessage>();
            var react = new InAction<TMessage>((in TMessage message) => controller.Run(new Phases.React<TMessage> { Message = message }));
            return new[]
            {
                Phase.From((in Phases.React<TMessage> phase) => run(phase.Message)),
                Phase.From((in Phases.React.Initialize _) => reaction.Add(react), Phase.Targets.Root, typeof(TMessage)),
                Phase.From((in Phases.React.Dispose _) => reaction.Remove(react), Phase.Targets.Root, typeof(TMessage))
            };
        }

        public abstract InAction<TMessage> Run(in T instance, in Context context);
    }

    public sealed class OnMessage<T> : React<IReact<T>, T> where T : struct, IMessage
    {
        public override InAction<T> Run(in IReact<T> instance, in Context context) => instance.React;
    }

    public sealed class OnAdd<T> : React<IOnAdd<T>, Messages.OnAdd<T>> where T : struct, IComponent
    {
        delegate void Runner(Entity entity, ref T component);

        public override InAction<Messages.OnAdd<T>> Run(in IOnAdd<T> instance, in Context context)
        {
            var components = context.Controller.World.Components();
            var run = new Runner(instance.OnAdd);
            return (in Messages.OnAdd<T> message) =>
            {
                if (components.TryStore<T>(message.Entity, out var store, out var index))
                    run(message.Entity, ref store[index]);
            };
        }
    }

    public sealed class OnRemove<T> : React<IOnRemove<T>, Messages.OnRemove<T>> where T : struct, IComponent
    {
        delegate void Runner(Entity entity, ref T component);

        public override InAction<Messages.OnRemove<T>> Run(in IOnRemove<T> instance, in Context context)
        {
            var components = context.Controller.World.Components();
            var run = new Runner(instance.OnRemove);
            return (in Messages.OnRemove<T> message) =>
            {
                if (components.TryStore<T>(message.Entity, out var store, out var index))
                    run(message.Entity, ref store[index]);
            };
        }
    }

    public sealed class OnEnable<T> : React<IOnEnable<T>, Messages.OnEnable<T>> where T : struct, IComponent
    {
        delegate void Runner(Entity entity, ref T component);

        public override InAction<Messages.OnEnable<T>> Run(in IOnEnable<T> instance, in Context context)
        {
            var components = context.Controller.World.Components();
            var run = new Runner(instance.OnEnable);
            return (in Messages.OnEnable<T> message) =>
            {
                if (components.TryStore<T>(message.Entity, out var store, out var index))
                    run(message.Entity, ref store[index]);
            };
        }
    }

    public sealed class OnDisable<T> : React<IOnDisable<T>, Messages.OnDisable<T>> where T : struct, IComponent
    {
        delegate void Runner(Entity entity, ref T component);

        public override InAction<Messages.OnDisable<T>> Run(in IOnDisable<T> instance, in Context context)
        {
            var components = context.Controller.World.Components();
            var run = new Runner(instance.OnDisable);
            return (in Messages.OnDisable<T> message) =>
            {
                if (components.TryStore<T>(message.Entity, out var store, out var index))
                    run(message.Entity, ref store[index]);
            };
        }
    }
}
