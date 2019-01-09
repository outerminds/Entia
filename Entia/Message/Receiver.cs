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

    public sealed class Receiver<T> : IReceiver
        where T : struct, IMessage
    {
        public int Count => _messages.ReadCount();
        public int Capacity
        {
            get => _capacity;
            set => Trim(_capacity = value);
        }

        Type IReceiver.Type => typeof(T);

        // TODO: is this thread safe?
        int _capacity;

        readonly Concurrent<Queue<T>> _messages = new Queue<T>();

        public Receiver(int capacity = -1) { _capacity = capacity; }

        public bool TryPop(out T message)
        {
            using (var write = _messages.Write()) return write.Value.TryDequeue(out message);
        }

        public bool Clear()
        {
            using (var write = _messages.Write())
            {
                var cleared = write.Value.Count > 0;
                write.Value.Clear();
                return cleared;
            }
        }

        public void Receive(in T message)
        {
            if (_capacity == 0) return;

            using (var write = _messages.Write())
            {
                write.Value.Enqueue(message);
                Trim(Capacity);
            }
        }

        void Trim(int count)
        {
            using (var write = _messages.Write())
                if (count >= 0) while (write.Value.Count > count) write.Value.Dequeue();
        }
    }
}
