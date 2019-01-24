using System;
using System.Collections.Generic;
using Entia.Core;

namespace Entia.Modules.Message
{
    public interface IEmitter
    {
        IEnumerable<IReaction> Reactions { get; }
        IEnumerable<IReceiver> Receivers { get; }
        Type Type { get; }

        bool Emit(IMessage message);
        bool Has(IReaction reaction);
        bool Add(IReaction reaction);
        bool Remove(IReaction reaction);
        bool Has(IReceiver receiver);
        bool Add(IReceiver receiver);
        bool Remove(IReceiver receiver);
        bool Clear();
    }

    public sealed class Emitter<T> : IEmitter where T : struct, IMessage
    {
        public Slice<Reaction<T>>.Read Reactions => _reactions.Slice();
        public Slice<Receiver<T>>.Read Receivers => _receivers.Slice();

        IEnumerable<IReaction> IEmitter.Reactions => Reactions;
        IEnumerable<IReceiver> IEmitter.Receivers => Receivers;
        Type IEmitter.Type => typeof(T);

        (Reaction<T>[] items, int count) _reactions = (new Reaction<T>[2], 0);
        (Receiver<T>[] items, int count) _receivers = (new Receiver<T>[2], 0);

        [ThreadSafe]
        public void Emit(in T message)
        {
            for (var i = 0; i < _reactions.count; i++) _reactions.items[i].React(message);
            for (var i = 0; i < _receivers.count; i++) _receivers.items[i].Receive(message);
        }

        public bool Add(Reaction<T> reaction)
        {
            if (Has(reaction)) return false;
            _reactions.Push(reaction);
            return true;
        }

        public bool Add(Receiver<T> receiver)
        {
            if (Has(receiver)) return false;
            _receivers.Push(receiver);
            return true;
        }

        public bool Has(Reaction<T> reaction) => _reactions.Contains(reaction);
        public bool Has(Receiver<T> receiver) => _receivers.Contains(receiver);
        public bool Remove(Reaction<T> reaction) => _reactions.Remove(reaction);
        public bool Remove(Receiver<T> receiver) => _receivers.Remove(receiver);

        public bool Clear() => _reactions.Clear() | _receivers.Clear();

        bool IEmitter.Emit(IMessage message)
        {
            if (message is T casted)
            {
                Emit(casted);
                return true;
            }
            return false;
        }
        bool IEmitter.Add(IReaction reaction) => reaction is Reaction<T> casted && Add(casted);
        bool IEmitter.Add(IReceiver receiver) => receiver is Receiver<T> casted && Add(casted);
        bool IEmitter.Has(IReaction reaction) => reaction is Reaction<T> casted && Has(casted);
        bool IEmitter.Has(IReceiver receiver) => receiver is Receiver<T> casted && Has(casted);
        bool IEmitter.Remove(IReaction reaction) => reaction is Reaction<T> casted && Remove(casted);
        bool IEmitter.Remove(IReceiver receiver) => receiver is Receiver<T> casted && Remove(casted);
    }
}
