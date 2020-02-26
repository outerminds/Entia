using Entia.Core;
using Entia.Core.Documentation;
using Entia.Modules.Message;
using Entia.Experimental.Serializers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;

namespace Entia.Modules
{
    [ThreadSafe]
    public sealed class Messages : IModule, IClearable, IEnumerable<IEmitter>
    {
        [Implementation]
        static Serializer<Messages> _serializer => Serializer.Object(
            () => new Messages(),
            Serializer.Member.Property(
                (in Messages messages) => messages._emitters.Values.ToArray(),
                (ref Messages messages, in IEmitter[] emitters) => { foreach (var emitter in emitters) messages._emitters.TryAdd(emitter.Type, emitter); },
                Serializer.Array<IEmitter>())
        );

        readonly ConcurrentDictionary<Type, IEmitter> _emitters = new ConcurrentDictionary<Type, IEmitter>();

        public Emitter<T> Emitter<T>() where T : struct, IMessage =>
            (Emitter<T>)_emitters.GetOrAdd(typeof(T), _ => new Emitter<T>());
        public IEmitter Emitter(Type type) =>
            _emitters.GetOrAdd(type, key => (IEmitter)Activator.CreateInstance(typeof(Emitter<>).MakeGenericType(key)));

        public bool Emit<T>() where T : struct, IMessage
        {
            if (TryEmitter<T>(out var emitter))
            {
                emitter.Emit();
                return true;
            }

            return false;
        }

        public bool Emit<T>(in T message) where T : struct, IMessage
        {
            if (TryEmitter<T>(out var emitter))
            {
                emitter.Emit(message);
                return true;
            }

            return false;
        }

        public bool Emit(Type type)
        {
            if (TryEmitter(type, out var emitter))
            {
                emitter.Emit();
                return true;
            }

            return false;
        }

        public bool Emit(IMessage message) => TryEmitter(message.GetType(), out var emitter) && emitter.Emit(message);

        public Emitter<T>.Disposable Receive<T>(int capacity = -1) where T : struct, IMessage => Emitter<T>().Receive(capacity);

        public Receiver<T> Receiver<T>(int capacity = -1) where T : struct, IMessage
        {
            var receiver = new Receiver<T>(capacity);
            Emitter<T>().Add(receiver);
            return receiver;
        }

        public IReceiver Receiver(Type type, int capacity = -1)
        {
            var generic = typeof(Receiver<>).MakeGenericType(type);
            var receiver = Activator.CreateInstance(generic, capacity) as IReceiver;
            Emitter(type).Add(receiver);
            return receiver;
        }

        public Reaction<T> Reaction<T>() where T : struct, IMessage => Emitter<T>().Reaction;
        public IReaction Reaction(Type type) => Emitter(type).Reaction;
        public void React<T>(InAction<T> reaction) where T : struct, IMessage => Reaction<T>().Add(reaction);
        public bool React(Type type, Delegate reaction) => Reaction(type).Add(reaction);

        public bool Has<T>() where T : struct, IMessage => Has(typeof(T));
        public bool Has(Type type) => _emitters.ContainsKey(type);
        public bool Has(IEmitter emitter) => TryEmitter(emitter.Type, out var value) && emitter == value;
        public bool Has<T>(Emitter<T> emitter) where T : struct, IMessage => TryEmitter<T>(out var value) && emitter == value;
        public bool Has(IReceiver receiver) => TryEmitter(receiver.Type, out var emitter) && emitter.Has(receiver);
        public bool Has<T>(Receiver<T> receiver) where T : struct, IMessage => TryEmitter<T>(out var emitter) && emitter.Has(receiver);

        public bool Remove<T>(Emitter<T> emitter) where T : struct, IMessage => Has(emitter) && _emitters.TryRemove(typeof(T), out _);
        public bool Remove(IEmitter emitter) => Has(emitter) && _emitters.TryRemove(emitter.Type, out _);
        public bool Remove<T>(Receiver<T> receiver) where T : struct, IMessage => TryEmitter<T>(out var emitter) && emitter.Remove(receiver);
        public bool Remove(IReceiver receiver) => TryEmitter(receiver.Type, out var emitter) && emitter.Remove(receiver);
        public bool Remove<T>(InAction<T> reaction) where T : struct, IMessage => TryEmitter<T>(out var emitter) && emitter.Reaction.Remove(reaction);
        public bool Remove(Type type, Delegate reaction) => TryEmitter(type, out var emitter) && emitter.Reaction.Remove(reaction);
        public bool Remove<T>() where T : struct, IMessage => TryEmitter<T>(out var emitter) && Remove(emitter);
        public bool Remove(Type type) => TryEmitter(type, out var emitter) && Remove(emitter);

        public bool Clear()
        {
            var cleared = false;
            foreach (var pair in _emitters) cleared |= pair.Value.Clear();
            return cleared;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<IEmitter> GetEnumerator() => _emitters.Values.GetEnumerator();
        IEnumerator<IEmitter> IEnumerable<IEmitter>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool TryEmitter<T>(out Emitter<T> emitter) where T : struct, IMessage
        {
            if (TryEmitter(typeof(T), out var value) && value is Emitter<T> casted)
            {
                emitter = casted;
                return true;
            }

            emitter = default;
            return false;
        }

        bool TryEmitter(Type type, out IEmitter emitter) => _emitters.TryGetValue(type, out emitter);
    }
}
