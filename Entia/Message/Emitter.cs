using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Entia.Core;
using Entia.Core.Documentation;

namespace Entia.Modules.Message
{
    [ThreadSafe]
    public interface IEmitter : IEnumerable<IReceiver>
    {
        IReaction Reaction { get; }
        Type Type { get; }

        void Emit();
        bool Emit(IMessage message);
        bool Has(IReceiver receiver);
        bool Add(IReceiver receiver);
        bool Remove(IReceiver receiver);
        bool Clear();
    }

    [ThreadSafe]
    public sealed class Emitter<T> : IEmitter, IEnumerable<Receiver<T>> where T : struct, IMessage
    {
        public struct Disposable : IDisposable
        {
            public int Count => _receiver.Count;

            readonly Emitter<T> _emitter;
            readonly Receiver<T> _receiver;

            public bool TryPop(out T message) => _receiver.TryPop(out message);
            public Receiver<T>.Enumerable Pop() => _receiver.Pop();

            public Disposable(Emitter<T> emitter, int capacity = -1)
            {
                _emitter = emitter;
                _receiver = new Receiver<T>(capacity);
                _emitter.Add(_receiver);
            }

            public void Dispose() => _emitter.Remove(_receiver);
        }

        static readonly InFunc<T, bool> _empty = (in T _) => false;

        public Reaction<T> Reaction { get; } = new Reaction<T>();

        IReaction IEmitter.Reaction => Reaction;
        Type IEmitter.Type => typeof(T);

        event InFunc<T, bool> _receive = _empty;
        ConcurrentDictionary<Receiver<T>, Unit> _receivers = new ConcurrentDictionary<Receiver<T>, Unit>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Emit() => Emit(DefaultUtility.Default<T>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Emit(in T message)
        {
            Reaction.React(message);
            _receive(message);
        }

        public Disposable Receive(int capacity = -1) => new Disposable(this, capacity);

        public bool Add(Receiver<T> receiver)
        {
            if (_receivers.TryAdd(receiver, default))
            {
                _receive += receiver.Receive;
                return true;
            }
            return false;
        }

        public bool Has(Receiver<T> receiver) => _receivers.ContainsKey(receiver);

        public bool Remove(Receiver<T> receiver)
        {
            if (_receivers.TryRemove(receiver, out _))
            {
                _receive -= receiver.Receive;
                return true;
            }

            return false;
        }

        public bool Clear()
        {
            var cleared = Reaction.Clear();
            _receivers.Clear();
            return _receive != Concurrent.Mutate(ref _receive, _empty) || cleared;
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

        public IEnumerator<Receiver<T>> GetEnumerator() => _receivers.Keys.GetEnumerator();
        IEnumerator<IReceiver> IEnumerable<IReceiver>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        bool IEmitter.Add(IReceiver receiver) => receiver is Receiver<T> casted && Add(casted);
        bool IEmitter.Has(IReceiver receiver) => receiver is Receiver<T> casted && Has(casted);
        bool IEmitter.Remove(IReceiver receiver) => receiver is Receiver<T> casted && Remove(casted);

    }
}
