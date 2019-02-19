using System;
using System.Collections.Generic;
using System.Threading;
using Entia.Core;
using Entia.Core.Documentation;

namespace Entia.Modules.Message
{
    [ThreadSafe]
    public interface IEmitter
    {
        IReaction Reaction { get; }
        IReceiver[] Receivers { get; }
        Type Type { get; }

        void Emit();
        bool Emit(IMessage message);
        bool Has(IReceiver receiver);
        bool Add(IReceiver receiver);
        bool Remove(IReceiver receiver);
        bool Clear();
    }

    [ThreadSafe]
    public sealed class Emitter<T> : IEmitter where T : struct, IMessage
    {
        public Reaction<T> Reaction { get; } = new Reaction<T>();
        public Receiver<T>[] Receivers => _receivers.Read(receivers => receivers.ToArray());

        IReaction IEmitter.Reaction => Reaction;
        IReceiver[] IEmitter.Receivers => Receivers;
        Type IEmitter.Type => typeof(T);

        Concurrent<(Receiver<T>[] items, int count)> _receivers = (new Receiver<T>[2], 0);

        public void Emit() => Emit(DefaultUtility.Cache<T>.Provide());

        public void Emit(in T message)
        {
            Reaction.React(message);
            using (var read = _receivers.Read())
                for (var i = 0; i < read.Value.count; i++) read.Value.items[i].Receive(message);
        }

        public bool Add(Receiver<T> receiver)
        {
            using (var write = _receivers.Write())
            {
                if (write.Value.Contains(receiver)) return false;
                write.Value.Push(receiver);
                return true;
            }
        }

        public bool Has(Receiver<T> receiver)
        {
            using (var read = _receivers.Read()) return read.Value.Contains(receiver);
        }

        public bool Remove(Receiver<T> receiver)
        {
            using (var write = _receivers.Write()) return write.Value.Remove(receiver);
        }

        public bool Clear()
        {
            var cleared = Reaction.Clear();
            using (var write = _receivers.Write())
            {
                foreach (var receiver in write.Value.Slice()) cleared |= receiver.Clear();
                cleared |= write.Value.Clear();
            }
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
