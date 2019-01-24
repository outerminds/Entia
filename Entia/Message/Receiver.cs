using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Entia.Core;

namespace Entia.Modules.Message
{
    public interface IReceiver
    {
        Type Type { get; }
        int Count { get; }
        int Capacity { get; set; }

        bool Clear();
    }

    public sealed class Receiver<T> : IReceiver where T : struct, IMessage
    {
        public int Count => _messages.Count;
        public int Capacity
        {
            get => _capacity;
            set => Trim(_capacity = value);
        }

        Type IReceiver.Type => typeof(T);

        readonly ConcurrentQueue<T> _messages = new ConcurrentQueue<T>();
        int _capacity;

        public Receiver(int capacity = -1) { _capacity = capacity; }

        public bool TryPop(out T message) => _messages.TryDequeue(out message);

        public bool Clear()
        {
            var cleared = false;
            while (_messages.TryDequeue(out _)) cleared = true;
            return cleared;
        }

        [ThreadSafe]
        public void Receive(in T message)
        {
            if (_capacity == 0) return;
            _messages.Enqueue(message);
            Trim(Capacity);
        }

        void Trim(int count)
        {
            if (count < 0) return;
            // NOTE: a lock is needed in case multiple threads pass the while condition even though only 1 item remains to be dequeued
            lock (_messages) { while (_messages.Count > count) _messages.TryDequeue(out _); }
        }
    }
}
