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
    public interface IReceiver
    {
        IMessage[] Messages { get; }
        Type Type { get; }
        int Count { get; }
        int Capacity { get; set; }

        bool TryPop(out IMessage message);
        IEnumerable<IMessage> Pop();
        bool Receive(IMessage message);
        bool Clear();
    }

    [ThreadSafe]
    public sealed class Receiver<T> : IReceiver where T : struct, IMessage
    {
        [ThreadSafe]
        public readonly struct Enumerable : IEnumerable<Enumerator, T>
        {
            readonly Receiver<T> _receiver;
            public Enumerable(Receiver<T> receiver) { _receiver = receiver; }

            public Enumerator GetEnumerator() => new Enumerator(_receiver);
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [ThreadSafe]
        public struct Enumerator : IEnumerator<T>
        {
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }
            object IEnumerator.Current => Current;

            Receiver<T> _receiver;
            T _current;

            public Enumerator(Receiver<T> receiver)
            {
                _receiver = receiver;
                _current = default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => _receiver._messages.TryDequeue(out _current);
            public void Reset() => throw new NotSupportedException();
            public void Dispose() => _receiver = default;
        }

        public T[] Messages => _messages.ToArray();
        public int Count => _messages.Count;
        public int Capacity
        {
            get => _capacity;
            set => Trim(_capacity = value);
        }

        IMessage[] IReceiver.Messages => Messages.Cast<IMessage>().ToArray();
        Type IReceiver.Type => typeof(T);

        readonly ConcurrentQueue<T> _messages = new ConcurrentQueue<T>();
        int _capacity;

        public Receiver(int capacity = -1) { _capacity = capacity; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out T message) => _messages.TryDequeue(out message);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerable Pop() => new Enumerable(this);

        public bool Receive(in T message)
        {
            if (_capacity == 0) return false;
            _messages.Enqueue(message);
            Trim(_capacity);
            return true;
        }

        public bool Clear()
        {
            var cleared = false;
            while (_messages.TryDequeue(out _)) cleared = true;
            return cleared;
        }

        void Trim(int count)
        {
            if (count < 0 || _messages.Count <= count) return;
            // NOTE: a lock is needed in case multiple threads pass the while condition even though only 1 item remains to be dequeued
            lock (_messages) { while (_messages.Count > count) _messages.TryDequeue(out _); }
        }

        bool IReceiver.TryPop(out IMessage message)
        {
            if (TryPop(out var casted))
            {
                message = casted;
                return true;
            }

            message = default;
            return false;
        }

        IEnumerable<IMessage> IReceiver.Pop() => Pop().Cast<IMessage>();
        bool IReceiver.Receive(IMessage message) => message is T casted && Receive(casted);
    }
}
