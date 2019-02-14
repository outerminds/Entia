using Entia.Core;
using Entia.Modules;
using Entia.Modules.Message;
using FsCheck;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Test
{
    public sealed class EmitMessage<T> : Action<World, Model> where T : struct, IMessage
    {
        int _local;
        int _global;
        Emitter<T> _emitter;
        Receiver<T> _receiver;
        Reaction<T> _reaction;

        public override bool Pre(World value, Model model)
        {
            _emitter = value.Messages().Emitter<T>();
            _receiver = value.Messages().Receiver<T>();
            _reaction = value.Messages().Reaction<T>();
            _reaction.Add(LocalAdd);
            value.Messages().React<T>(GlobalAdd);
            return true;
        }

        void LocalAdd(in T message) => _local++;
        void GlobalAdd(in T message) => _global++;

        public override void Do(World value, Model model)
        {
            _emitter.Emit(default);
            value.Messages().Emit<T>(default);
            value.Messages().Emit((IMessage)default(T));
        }

        public override Property Check(World value, Model model) =>
            value.Messages().Has<T>().Label("Messages.Has<T>()")
            .And(value.Messages().Has(_emitter).Label("Messages.Has(Emitter<T>)"))
            .And(value.Messages().Has(_emitter as IEmitter).Label("Messages.Has(IEmitter)"))
            .And(value.Messages().Has(_receiver).Label("Messages.Has(Receiver<T>)"))
            .And(value.Messages().Has(_receiver as IReceiver).Label("Messages.Has(IReceiver)"))
            .And((value.Messages().Emitter<T>() == _emitter).Label("Messages.Emitter<T>()"))
            .And((value.Messages().Emitter(typeof(T)) == _emitter).Label("Messages.Emitter()"))

            .And(_emitter.Has(_receiver).Label("emitter.Has(Receiver<T>)"))
            .And((_emitter.Reaction == _reaction).Label("emitter.Reaction == reaction"))
            .And(_emitter.Receivers.Contains(_receiver).Label("emitter.Receivers.Contains()"))
            .And(_emitter.Add(_receiver).Not().Label("emitter.Has(Receiver<T>)"))

            .And((_local == 3).Label("local == 3"))
            .And((_global == 3).Label("global == 3"))
            .And((_receiver.Count == 3).Label("receiver.Count == 3"))
            .And((_receiver.TryPop(out _)).Label("receiver.TryPop(3)"))
            .And((_receiver.Count == 2).Label("receiver.Count == 2"))
            .And((_receiver.Clear()).Label("receiver.Clear(2)"))
            .And((_receiver.Count == 0).Label("receiver.Count == 0"))
            .And(_receiver.Clear().Not().Label("receiver.Clear(0)"))
            .And(_receiver.TryPop(out _).Not().Label("receiver.TryPop(0)"))

            .And(value.Messages().Remove<T>(GlobalAdd).Label("Messages.Remove<T>(GlobalAdd)"))
            .And(_reaction.Remove(LocalAdd).Label("reaction.Remove(LocalAdd)"))
            .And(_reaction.Clear().Not().Label("reaction.Clear()"))
            .And(_emitter.Remove(_receiver).Label("emitter.Remove(Receiver<T>)"));

        public override string ToString() => GetType().Format();
    }
}
