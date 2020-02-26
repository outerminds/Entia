using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Experimental.Serializers;

namespace Entia.Modules.Message
{
    public interface IReceiver
    {
        Type Type { get; }
        int Count { get; }
        int Capacity { get; set; }

        bool TryMessage(out IMessage message);
        IEnumerable<IMessage> Messages(int count = int.MaxValue);
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
            readonly int _count;

            public Enumerable(Receiver<T> receiver, int count)
            {
                _receiver = receiver;
                _count = count;
            }

            public Enumerator GetEnumerator() => new Enumerator(_receiver, _count);
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

            readonly int _count;
            readonly Receiver<T> _receiver;
            int _index;
            T _current;

            public Enumerator(Receiver<T> receiver, int count)
            {
                _receiver = receiver;
                _count = count;
                _index = -1;
                _current = default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _count && _receiver._messages.TryDequeue(out _current);
            public void Reset() => _index = -1;
            public void Dispose() => this = default;
        }

        [Implementation]
        static Serializer<Receiver<T>> _serializer => Serializer.Object(
            () => new Receiver<T>(),
            Serializer.Member.Field((in Receiver<T> receiver) => ref receiver._capacity),
            Serializer.Member.Property(
                (in Receiver<T> receiver) => receiver._messages.ToArray(),
                (ref Receiver<T> receiver, in T[] messages) => { for (int i = 0; i < messages.Length; i++) receiver._messages.Enqueue(messages[i]); })
        );

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

        [Obsolete("Use " + nameof(TryMessage) + " instead.")]
        public bool TryPop(out T message) => TryMessage(out message);
        [Obsolete("Use " + nameof(Messages) + " instead.")]
        public Enumerable Pop(int count = int.MaxValue) => Messages();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryMessage(out T message) => _messages.TryDequeue(out message);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerable Messages(int count = int.MaxValue) => new Enumerable(this, count);

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

        bool IReceiver.TryMessage(out IMessage message)
        {
            if (TryMessage(out var casted))
            {
                message = casted;
                return true;
            }

            message = default;
            return false;
        }

        IEnumerable<IMessage> IReceiver.Messages(int count) => Messages(count).Cast<IMessage>();
        bool IReceiver.Receive(IMessage message) => message is T casted && Receive(casted);
    }
}
