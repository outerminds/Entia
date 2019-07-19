using System;
using System.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Serializers;
using Entia.Serializables;

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
    public sealed class Emitter<T> : IEmitter, ISerializable<Emitter<T>.Serializer>, IEnumerable<Receiver<T>> where T : struct, IMessage
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

        sealed class Serializer : Serializer<Emitter<T>>
        {
            public override bool Serialize(in Emitter<T> instance, TypeData dynamic, TypeData @static, in WriteContext context)
            {
                var success = context.Serializers.Serialize(instance.Reaction, context);
                var receivers = instance._receivers.Keys.ToArray();
                context.Writer.Write(receivers.Length);
                for (int i = 0; i < receivers.Length; i++) success &= context.Serializers.Serialize(receivers[i], context);
                return success;
            }

            public override bool Instantiate(out Emitter<T> instance, TypeData dynamic, TypeData @static, in ReadContext context)
            {
                instance = new Emitter<T>();
                return true;
            }

            public override bool Deserialize(ref Emitter<T> instance, TypeData dynamic, TypeData @static, in ReadContext context)
            {
                var success = context.Serializers.Deserialize(out Reaction<T> reaction, context);
                UnsafeUtility.Set(instance.Reaction, reaction);
                context.Reader.Read(out int count);
                for (int i = 0; i < count; i++)
                {
                    success &= context.Serializers.Deserialize(out Receiver<T> receiver, context);
                    instance.Add(receiver);
                }
                return success;
            }
        }

        static readonly InFunc<T, bool> _empty = (in T _) => false;

        public readonly Reaction<T> Reaction = new Reaction<T>();

        IReaction IEmitter.Reaction => Reaction;
        Type IEmitter.Type => typeof(T);

        event InFunc<T, bool> _receive = _empty;
        readonly ConcurrentDictionary<Receiver<T>, Unit> _receivers = new ConcurrentDictionary<Receiver<T>, Unit>();

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
            var cleared = false;
            foreach (var receiver in _receivers.Keys) cleared |= receiver.Clear();
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

        public IEnumerator<Receiver<T>> GetEnumerator() => _receivers.Keys.GetEnumerator();
        IEnumerator<IReceiver> IEnumerable<IReceiver>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        bool IEmitter.Add(IReceiver receiver) => receiver is Receiver<T> casted && Add(casted);
        bool IEmitter.Has(IReceiver receiver) => receiver is Receiver<T> casted && Has(casted);
        bool IEmitter.Remove(IReceiver receiver) => receiver is Receiver<T> casted && Remove(casted);

    }
}
