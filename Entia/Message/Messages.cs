using Entia.Core;
using Entia.Core.Documentation;
using Entia.Modules.Message;
using Entia.Serializables;
using Entia.Serializers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    [ThreadSafe]
    public sealed class Messages : IModule, IClearable, ISerializable<Messages.Serializer>, IEnumerable<IEmitter>
    {
        readonly Concurrent<TypeMap<IMessage, IEmitter>> _emitters = new TypeMap<IMessage, IEmitter>();

        sealed class Serializer : Serializer<Messages>
        {
            public override bool Serialize(in Messages instance, TypeData dynamic, TypeData @static, in WriteContext context)
            {
                var success = true;
                var count = context.Writer.Reserve<uint>();
                using (var read = instance._emitters.Read())
                {
                    foreach (var emitter in read.Value.Values)
                    {
                        count.Value++;
                        success &= context.Serializers.Serialize(emitter, context);
                    }
                }
                return success;
            }
            public override bool Instantiate(out Messages instance, TypeData dynamic, TypeData @static, in ReadContext context)
            {
                instance = new Messages();
                return true;
            }
            public override bool Deserialize(ref Messages instance, TypeData dynamic, TypeData @static, in ReadContext context)
            {
                var success = context.Reader.Read(out uint count);
                using (var write = instance._emitters.Write())
                {
                    for (var i = 0u; i < count; i++)
                    {
                        if (context.Serializers.Deserialize(out IEmitter emitter, context))
                            write.Value[emitter.Type] = emitter;
                        else success = false;
                    }
                }
                return success;
            }
        }

        public Emitter<T> Emitter<T>() where T : struct, IMessage
        {
            using (var read = _emitters.Read(true))
            {
                if (read.Value.TryGet<T>(out var emitter) && emitter is Emitter<T> casted1) return casted1;
                using (var write = _emitters.Write())
                {
                    if (write.Value.TryGet<T>(out emitter) && emitter is Emitter<T> casted2) return casted2;
                    write.Value.Set<T>(casted2 = new Emitter<T>());
                    return casted2;
                }
            }
        }

        public IEmitter Emitter(Type type)
        {
            using (var read = _emitters.Read(true))
            {
                if (read.Value.TryGet(type, out var emitter)) return emitter;
                using (var write = _emitters.Write())
                {
                    if (write.Value.TryGet(type, out emitter)) return emitter;
                    var generic = typeof(Emitter<>).MakeGenericType(type);
                    write.Value.Set(type, emitter = Activator.CreateInstance(generic) as IEmitter);
                    return emitter;
                }
            }
        }

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

        public bool Has<T>() where T : struct, IMessage
        {
            using (var read = _emitters.Read()) return read.Value.Has<T>();
        }

        public bool Has(Type type)
        {
            using (var read = _emitters.Read()) return read.Value.Has(type);
        }

        public bool Has(IEmitter emitter)
        {
            using (var read = _emitters.Read()) return read.Value.TryGet(emitter.Type, out var value) && value == emitter;
        }

        public bool Has<T>(Emitter<T> emitter) where T : struct, IMessage
        {
            using (var read = _emitters.Read()) return read.Value.TryGet<T>(out var value) && value == emitter;
        }

        public bool Has(IReceiver receiver)
        {
            using (var read = _emitters.Read()) return read.Value.TryGet(receiver.Type, out var emitter) && emitter.Has(receiver);
        }

        public bool Has<T>(Receiver<T> receiver) where T : struct, IMessage
        {
            using (var read = _emitters.Read()) return read.Value.TryGet<T>(out var emitter) && emitter.Has(receiver);
        }

        public bool Remove<T>(Emitter<T> emitter) where T : struct, IMessage
        {
            using (var write = _emitters.Write())
            {
                if (Has(emitter) && write.Value.Remove<T>())
                {
                    emitter.Clear();
                    return true;
                }

                return false;
            }
        }

        public bool Remove(IEmitter emitter)
        {
            using (var write = _emitters.Write())
            {
                if (Has(emitter) && write.Value.Remove(emitter.Type))
                {
                    emitter.Clear();
                    return true;
                }

                return false;
            }
        }

        public bool Remove<T>(Receiver<T> receiver) where T : struct, IMessage => TryEmitter<T>(out var emitter) && emitter.Remove(receiver);
        public bool Remove(IReceiver receiver) => TryEmitter(receiver.Type, out var emitter) && emitter.Remove(receiver);
        public bool Remove<T>(InAction<T> reaction) where T : struct, IMessage => TryEmitter<T>(out var emitter) && emitter.Reaction.Remove(reaction);
        public bool Remove(Type type, Delegate reaction) => TryEmitter(type, out var emitter) && emitter.Reaction.Remove(reaction);
        public bool Remove<T>() where T : struct, IMessage => TryEmitter<T>(out var emitter) && Remove(emitter);
        public bool Remove(Type type) => TryEmitter(type, out var emitter) && Remove(emitter);

        public bool Clear()
        {
            var cleared = false;
            using (var write = _emitters.Write())
            {
                foreach (var emitter in write.Value.Values) cleared |= emitter.Clear();
                return cleared;
            }
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<IEmitter> GetEnumerator() => _emitters.Read(emitters => emitters.Values.ToArray()).Cast<IEmitter>().GetEnumerator();
        IEnumerator<IEmitter> IEnumerable<IEmitter>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool TryEmitter<T>(out Emitter<T> emitter) where T : struct, IMessage
        {
            using (var read = _emitters.Read())
            {
                if (read.Value.TryGet<T>(out var value) && value is Emitter<T> casted)
                {
                    emitter = casted;
                    return true;
                }

                emitter = default;
                return false;
            }
        }

        bool TryEmitter(Type type, out IEmitter emitter)
        {
            using (var read = _emitters.Read()) return read.Value.TryGet(type, out emitter);
        }
    }
}
