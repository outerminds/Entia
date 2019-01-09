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
		readonly List<T> _localReactions = new List<T>();
		readonly List<T> _globalReactions = new List<T>();
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

		void LocalAdd(in T message) => _localReactions.Add(message);
		void GlobalAdd(in T message) => _globalReactions.Add(message);

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
			.And(value.Messages().Has(_reaction).Label("Messages.Has(Reaction<T>)"))
			.And(value.Messages().Has(_reaction as IReaction).Label("Messages.Has(IReaction)"))
			.And((value.Messages().Emitter<T>() == _emitter).Label("Messages.Emitter<T>()"))
			.And((value.Messages().Emitter(typeof(T)) == _emitter).Label("Messages.Emitter()"))

			.And(_emitter.Has(_receiver).Label("emitter.Has(Receiver<T>)"))
			.And(_emitter.Has(_reaction).Label("emitter.Has(Reaction<T>)"))
			.And(_emitter.Receivers.Contains(_receiver).Label("emitter.Receivers.Contains()"))
			.And(_emitter.Reactions.Contains(_reaction).Label("emitter.Reactions.Contains()"))
			.And(_emitter.Add(_receiver).Not().Label("emitter.Has(Receiver<T>)"))
			.And(_emitter.Add(_reaction).Not().Label("emitter.Has(Reaction<T>)"))

			.And((_localReactions.Count == 3).Label("local.Count"))
			.And((_globalReactions.Count == 3).Label("global.Count"))
			.And((_receiver.Count == 3).Label("receiver.Count == 3"))
			.And((_receiver.TryPop(out _)).Label("receiver.TryPop(3)"))
			.And((_receiver.Count == 2).Label("receiver.Count == 2"))
			.And((_receiver.Clear()).Label("receiver.Clear(2)"))
			.And((_receiver.Count == 0).Label("receiver.Count == 0"))
			.And(_receiver.Clear().Not().Label("receiver.Clear(0)"))
			.And(_receiver.TryPop(out _).Not().Label("receiver.TryPop(0)"))

			.And(_reaction.Has(LocalAdd).Label("reaction.Has(LocalAdd)"))
			.And(_reaction.Has(GlobalAdd).Not().Label("reaction.Has(GlobalAdd)"))
			.And(_reaction.Add(LocalAdd).Not().Label("reaction.Add(LocalAdd)"))
			.And(_reaction.Contains((InAction<T>)LocalAdd).Label("reaction.Contains(LocalAdd)"))
			.And(_reaction.Contains((InAction<T>)GlobalAdd).Not().Label("reaction.Contains(GlobalAdd)"))
			.And(_reaction.Remove(LocalAdd).Label("reaction.Remove(LocalAdd)"))
			.And(_reaction.Remove(GlobalAdd).Not().Label("reaction.Remove(GlobalAdd)"))
			.And(value.Messages().Remove<T>(GlobalAdd).Label("Messages.Remove<T>(GlobalAdd)"))
			.And(_reaction.Clear().Not().Label("reaction.Clear()"))

			.And(_emitter.Remove(_receiver).Label("emitter.Remove(Receiver<T>)"))
			.And(_emitter.Remove(_reaction).Label("emitter.Remove(Reaction<T>)"));
	}
}
