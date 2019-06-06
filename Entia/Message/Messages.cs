using Entia.Core;
using Entia.Core.Documentation;
using Entia.Modules.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    [ThreadSafe]
    public sealed class Messages : IModule, IClearable, IEnumerable<IEmitter>
    {
        readonly Concurrent<TypeMap<IMessage, IEmitter>> _emitters = new TypeMap<IMessage, IEmitter>();

        public Emitter<T> Emitter<T>() where T : struct, IMessage
        {
            using (var read = _emitters.Read(true))
            {
                if (read.Value.TryGet<T>(out var emitter, false, false) && emitter is Emitter<T> casted1) return casted1;
                using (var write = _emitters.Write())
                {
                    if (write.Value.TryGet<T>(out emitter, false, false) && emitter is Emitter<T> casted2) return casted2;
                    write.Value.Set<T>(casted2 = new Emitter<T>());
                    return casted2;
                }
            }
        }

        public IEmitter Emitter(Type type)
        {
            using (var read = _emitters.Read(true))
            {
                if (read.Value.TryGet(type, out var emitter, false, false)) return emitter;
                using (var write = _emitters.Write())
                {
                    if (write.Value.TryGet(type, out emitter, false, false)) return emitter;
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
            using (var read = _emitters.Read()) return read.Value.Has<T>(false, false);
        }

        public bool Has(Type type)
        {
            using (var read = _emitters.Read()) return read.Value.Has(type, false, false);
        }

        public bool Has(IEmitter emitter)
        {
            using (var read = _emitters.Read()) return read.Value.TryGet(emitter.Type, out var value, false, false) && value == emitter;
        }

        public bool Has<T>(Emitter<T> emitter) where T : struct, IMessage
        {
            using (var read = _emitters.Read()) return read.Value.TryGet<T>(out var value, false, false) && value == emitter;
        }

        public bool Has(IReceiver receiver)
        {
            using (var read = _emitters.Read()) return read.Value.TryGet(receiver.Type, out var emitter, false, false) && emitter.Has(receiver);
        }

        public bool Has<T>(Receiver<T> receiver) where T : struct, IMessage
        {
            using (var read = _emitters.Read()) return read.Value.TryGet<T>(out var emitter, false, false) && emitter.Has(receiver);
        }

        public bool Remove<T>(Emitter<T> emitter) where T : struct, IMessage
        {
            using (var write = _emitters.Write())
            {
                if (Has(emitter) && write.Value.Remove<T>(false, false))
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
                if (Has(emitter) && write.Value.Remove(emitter.Type, false, false))
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
                cleared |= write.Value.Clear();
                return cleared;
            }
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Slice<IEmitter>.Read.Enumerator GetEnumerator() => _emitters.Read(emitters => emitters.Values.ToArray()).Slice().GetEnumerator();
        IEnumerator<IEmitter> IEnumerable<IEmitter>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool TryEmitter<T>(out Emitter<T> emitter) where T : struct, IMessage
        {
            using (var read = _emitters.Read())
            {
                if (read.Value.TryGet<T>(out var value, false, false) && value is Emitter<T> casted)
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
            using (var read = _emitters.Read()) return read.Value.TryGet(type, out emitter, false, false);
        }
    }
}
