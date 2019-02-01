using System;
using System.Collections.Generic;
using Entia.Core;
using Entia.Core.Documentation;

namespace Entia.Modules.Message
{
    public interface IEmitter
    {
        IReaction Reaction { get; }
        IEnumerable<IReceiver> Receivers { get; }
        Type Type { get; }

        bool Emit(IMessage message);
        bool Has(IReceiver receiver);
        bool Add(IReceiver receiver);
        bool Remove(IReceiver receiver);
        bool Clear();
    }

    public sealed class Emitter<T> : IEmitter where T : struct, IMessage
    {
        [ThreadSafe]
        public Reaction<T> Reaction { get; } = new Reaction<T>();
        [ThreadSafe]
        public Slice<Receiver<T>>.Read Receivers => _receivers.Slice();

        IReaction IEmitter.Reaction => Reaction;
        IEnumerable<IReceiver> IEmitter.Receivers => Receivers;
        Type IEmitter.Type => typeof(T);

        (Receiver<T>[] items, int count) _receivers = (new Receiver<T>[2], 0);

        [ThreadSafe]
        public void Emit(in T message)
        {
            Reaction.React(message);
            for (var i = 0; i < _receivers.count; i++) _receivers.items[i].Receive(message);
        }

        public bool Add(Receiver<T> receiver)
        {
            if (Has(receiver)) return false;
            _receivers.Push(receiver);
            return true;
        }

        [ThreadSafe]
        public bool Has(Receiver<T> receiver) => _receivers.Contains(receiver);
        public bool Remove(Receiver<T> receiver) => _receivers.Remove(receiver);
        public bool Clear()
        {
            var cleared = Reaction.Clear();
            foreach (var receiver in _receivers.Slice()) cleared |= receiver.Clear();
            return cleared;
        }

        bool IEmitter.Emit(IMessage message)
        {
            if (message is T casted)
            {
                Emit(casted);
                return true;
            }
            return false;
        }
        bool IEmitter.Add(IReceiver receiver) => receiver is Receiver<T> casted && Add(casted);
        bool IEmitter.Has(IReceiver receiver) => receiver is Receiver<T> casted && Has(casted);
        bool IEmitter.Remove(IReceiver receiver) => receiver is Receiver<T> casted && Remove(casted);
    }
}
