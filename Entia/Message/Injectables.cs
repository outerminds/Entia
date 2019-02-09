using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Injectors;
using Entia.Modules;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Injectables
{
    public readonly struct AllEmitters : IInjectable
    {
        [Injector]
        static readonly Injector<AllEmitters> _injector = Injector.From(world => new AllEmitters(world.Messages()));
        [Depender]
        static readonly IDepender _depender = Depender.From(new Emit(typeof(IMessage)), new Write(typeof(IMessage)));

        readonly Modules.Messages _messages;
        public AllEmitters(Modules.Messages messages) { _messages = messages; }

        [ThreadSafe]
        public bool Has<T>() where T : struct, IMessage => _messages.Has<T>();
        [ThreadSafe]
        public bool Has(Type type) => _messages.Has(type);
        [ThreadSafe]
        public bool Emit<T>(in T message) where T : struct, IMessage => _messages.Emit(message);
        [ThreadSafe]
        public bool Emit(IMessage message) => _messages.Emit(message);
        public bool Remove<T>() where T : struct, IMessage => _messages.Remove<T>();
        public bool Remove(Type type) => _messages.Remove(type);
        public bool Clear() => _messages.Clear();
    }

    [ThreadSafe]
    public readonly struct Emitter<T> : IInjectable where T : struct, IMessage
    {
        [Injector]
        static readonly Injector<Emitter<T>> _injector = Injector.From(world => new Emitter<T>(world.Messages().Emitter<T>()));
        [Depender]
        static readonly IDepender _depender = Depender.From<T>(new Emit(typeof(T)), new Write(typeof(T)));

        readonly Modules.Message.Emitter<T> _emitter;
        public Emitter(Modules.Message.Emitter<T> emitter) { _emitter = emitter; }
        public void Emit(in T message) => _emitter.Emit(message);
    }

    [ThreadSafe]
    public readonly struct Receiver<T> : IInjectable where T : struct, IMessage
    {
        [Injector]
        static readonly Injector<Receiver<T>> _injector = Injector.From(world => new Receiver<T>(world.Messages().Receiver<T>()));
        [Depender]
        static readonly IDepender _depender = Depender.From<T>(new Read(typeof(T)));

        public int Count => _receiver.Count;
        public int Capacity { get => _receiver.Capacity; set => _receiver.Capacity = value; }

        readonly Modules.Message.Receiver<T> _receiver;
        public Receiver(Modules.Message.Receiver<T> receiver) { _receiver = receiver; }
        public bool TryPop(out T message) => _receiver.TryPop(out message);
        public bool Clear() => _receiver.Clear();
    }

    public readonly struct Reaction<T> : IInjectable where T : struct, IMessage
    {
        [Injector]
        static readonly Injector<Reaction<T>> _injector = Injector.From(world => new Reaction<T>(world.Messages().Reaction<T>()));
        [Depender]
        static readonly IDepender _depender = Depender.From<T>(new React(typeof(T)));

        readonly Modules.Message.Reaction<T> _reaction;
        public Reaction(Modules.Message.Reaction<T> reaction) { _reaction = reaction; }
        [ThreadSafe]
        public void Add(InAction<T> reaction) => _reaction.Add(reaction);
        [ThreadSafe]
        public void Remove(InAction<T> reaction) => _reaction.Remove(reaction);
        public bool Clear() => _reaction.Clear();
    }
}
