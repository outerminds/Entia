﻿using Entia.Core;
using Entia.Core.Documentation;
using Entia.Modules.Message;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Modules
{
    public sealed class Messages : IModule, IEnumerable<IEmitter>
    {
        readonly TypeMap<IMessage, IEmitter> _emitters = new TypeMap<IMessage, IEmitter>();

        public Emitter<T> Emitter<T>() where T : struct, IMessage
        {
            if (_emitters.TryGet<T>(out var value) && value is Emitter<T> emitter) return emitter;
            _emitters.Set<T>(emitter = new Emitter<T>());
            return emitter;
        }

        public IEmitter Emitter(Type message)
        {
            if (_emitters.TryGet(message, out var emitter, true)) return emitter;
            var type = typeof(Emitter<>).MakeGenericType(message);
            _emitters.Set(message, emitter = Activator.CreateInstance(type) as IEmitter);
            return emitter;
        }

        [ThreadSafe]
        public bool Emit<T>(in T message) where T : struct, IMessage
        {
            if (_emitters.TryGet<T>(out var value) && value is Emitter<T> emitter)
            {
                emitter.Emit(message);
                return true;
            }

            return false;
        }

        [ThreadSafe]
        public bool Emit(IMessage message) => _emitters.TryGet(message.GetType(), out var emitter, true) && emitter.Emit(message);

        public Receiver<T> Receiver<T>(int capacity = -1) where T : struct, IMessage
        {
            var receiver = new Receiver<T>(capacity);
            Emitter<T>().Add(receiver);
            return receiver;
        }

        public IReceiver Receiver(Type message, int capacity = -1)
        {
            var type = typeof(Receiver<>).MakeGenericType(message);
            var receiver = Activator.CreateInstance(type, capacity) as IReceiver;
            Emitter(message).Add(receiver);
            return receiver;
        }

        public Reaction<T> Reaction<T>() where T : struct, IMessage => Emitter<T>().Reaction;
        public IReaction Reaction(Type message) => Emitter(message).Reaction;
        public void React<T>(InAction<T> reaction) where T : struct, IMessage => Reaction<T>().Add(reaction);
        public bool React(Type message, Delegate reaction) => Reaction(message).Add(reaction);

        [ThreadSafe]
        public bool Has<T>() where T : struct, IMessage => _emitters.Has<T>();
        [ThreadSafe]
        public bool Has(Type message) => _emitters.Has(message);
        [ThreadSafe]
        public bool Has(IEmitter emitter) => _emitters.TryGet(emitter.Type, out var value, true) && value == emitter;
        [ThreadSafe]
        public bool Has<T>(Emitter<T> emitter) where T : struct, IMessage => _emitters.TryGet<T>(out var value) && value == emitter;
        [ThreadSafe]
        public bool Has(IReceiver receiver) => _emitters.TryGet(receiver.Type, out var emitter, true) && emitter.Has(receiver);
        [ThreadSafe]
        public bool Has<T>(Receiver<T> receiver) where T : struct, IMessage => _emitters.TryGet<T>(out var emitter) && emitter.Has(receiver);

        public bool Remove<T>(Emitter<T> emitter) where T : struct, IMessage
        {
            if (Has(emitter) && _emitters.Remove<T>())
            {
                emitter.Clear();
                return true;
            }

            return false;
        }
        public bool Remove(IEmitter emitter)
        {
            if (Has(emitter) && _emitters.Remove(emitter.Type))
            {
                emitter.Clear();
                return true;
            }

            return false;
        }
        public bool Remove<T>(Receiver<T> receiver) where T : struct, IMessage => _emitters.TryGet<T>(out var emitter) && emitter.Remove(receiver);
        public bool Remove(IReceiver receiver) => _emitters.TryGet(receiver.Type, out var emitter, true) && emitter.Remove(receiver);
        public bool Remove<T>(InAction<T> reaction) where T : struct, IMessage => _emitters.TryGet<T>(out var emitter) && emitter.Reaction.Remove(reaction);
        public bool Remove(Type message, Delegate reaction) => _emitters.TryGet(message, out var emitter, true) && emitter.Reaction.Remove(reaction);
        public bool Remove<T>() where T : struct, IMessage => _emitters.TryGet<T>(out var emitter) && Remove(emitter);
        public bool Remove(Type message) => _emitters.TryGet(message, out var emitter, true) && Remove(emitter);

        public bool Clear()
        {
            foreach (var (_, emitter) in _emitters) emitter.Clear();
            return _emitters.Clear();
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        [ThreadSafe]
        public TypeMap<IMessage, IEmitter>.ValueEnumerator GetEnumerator() => _emitters.Values.GetEnumerator();
        IEnumerator<IEmitter> IEnumerable<IEmitter>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
