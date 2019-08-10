using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Injectors;
using Entia.Modules;
using System;

namespace Entia.Injectables
{
    [ThreadSafe]
    public readonly struct AllEmitters : IInjectable
    {
        [Implementation]
        static Injector<AllEmitters> Injector => Injectors.Injector.From(context => new AllEmitters(context.World.Messages()));
        [Implementation]
        static IDepender Depender => Dependers.Depender.From(new Emit(typeof(IMessage)), new Write(typeof(IMessage)));

        readonly Modules.Messages _messages;
        public AllEmitters(Modules.Messages messages) { _messages = messages; }

        public bool Has<T>() where T : struct, IMessage => _messages.Has<T>();
        public bool Has(Type type) => _messages.Has(type);
        public bool Emit<T>(in T message) where T : struct, IMessage => _messages.Emit(message);
        public bool Emit(IMessage message) => _messages.Emit(message);
        public bool Emit(Type message) => _messages.Emit(message);

        // NOTE: do not give easy access to 'Remove/Clear' methods since they may have unwanted side-effects (such as Emitter<OnPreDestroy>.Clear())
    }

    [ThreadSafe]
    public readonly struct Emitter<T> : IInjectable where T : struct, IMessage
    {
        [Implementation]
        static Injector<object> Injector => Injectors.Injector.From<object>(context => new Emitter<T>(context.World.Messages().Emitter<T>()));
        [Implementation]
        static IDepender Depender => Dependers.Depender.From<T>(new Emit(typeof(T)), new Write(typeof(T)));

        readonly Modules.Message.Emitter<T> _emitter;
        public Emitter(Modules.Message.Emitter<T> emitter) { _emitter = emitter; }
        public void Emit() => _emitter.Emit();
        public void Emit(in T message) => _emitter.Emit(message);

        // NOTE: do not give easy access to 'Remove/Clear' methods since they may have unwanted side-effects (such as Emitter<OnPreDestroy>.Clear())
    }

    [ThreadSafe]
    public readonly struct Receiver<T> : IInjectable where T : struct, IMessage
    {
        [Implementation]
        static Injector<object> Injector => Injectors.Injector.From<object>(context => new Receiver<T>(context.World.Messages().Receiver<T>()));
        [Implementation]
        static IDepender Depender => Dependers.Depender.From<T>(new Read(typeof(T)));

        public int Count => _receiver.Count;
        public int Capacity { get => _receiver.Capacity; set => _receiver.Capacity = value; }

        readonly Modules.Message.Receiver<T> _receiver;
        public Receiver(Modules.Message.Receiver<T> receiver) { _receiver = receiver; }
        public bool TryPop(out T message) => _receiver.TryPop(out message);
        public Modules.Message.Receiver<T>.Enumerable Pop(int count = int.MaxValue) => _receiver.Pop(count);
        public bool Clear() => _receiver.Clear();
    }

    [ThreadSafe]
    public readonly struct Reaction<T> : IInjectable where T : struct, IMessage
    {
        [Implementation]
        static Injector<object> Injector => Injectors.Injector.From<object>(context => new Reaction<T>(context.World.Messages().Reaction<T>()));
        [Implementation]
        static IDepender Depender => Dependers.Depender.From<T>(new React(typeof(T)));

        readonly Modules.Message.Reaction<T> _reaction;
        public Reaction(Modules.Message.Reaction<T> reaction) { _reaction = reaction; }
        public void Add(InAction<T> reaction) => _reaction.Add(reaction);
        public void Remove(InAction<T> reaction) => _reaction.Remove(reaction);
        public bool Clear() => _reaction.Clear();
    }
}
