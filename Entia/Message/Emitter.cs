using System;
using System.Collections.Generic;

namespace Entia.Modules.Message
{
	public interface IEmitter
	{
		IEnumerable<IReaction> Reactions { get; }
		IEnumerable<IReceiver> Receivers { get; }
		Type Type { get; }

		bool Emit(IMessage message);
		bool Has(IReaction reaction);
		bool Add(IReaction reaction);
		bool Remove(IReaction reaction);
		bool Has(IReceiver receiver);
		bool Add(IReceiver receiver);
		bool Remove(IReceiver receiver);
		bool Clear();
	}

	public sealed class Emitter<T> : IEmitter where T : struct, IMessage
	{
		public IEnumerable<Reaction<T>> Reactions => _reactions;
		public IEnumerable<Receiver<T>> Receivers => _receivers;

		IEnumerable<IReaction> IEmitter.Reactions => Reactions;
		IEnumerable<IReceiver> IEmitter.Receivers => Receivers;
		Type IEmitter.Type => typeof(T);

		readonly List<Reaction<T>> _reactions = new List<Reaction<T>>();
		readonly List<Receiver<T>> _receivers = new List<Receiver<T>>();

		public void Emit(in T message)
		{
			for (var i = 0; i < _reactions.Count; i++) _reactions[i].React(message);
			for (var i = 0; i < _receivers.Count; i++) _receivers[i].Receive(message);
		}

		public bool Add(Reaction<T> reaction)
		{
			if (Has(reaction)) return false;
			_reactions.Add(reaction);
			return true;
		}

		public bool Add(Receiver<T> receiver)
		{
			if (Has(receiver)) return false;
			_receivers.Add(receiver);
			return true;
		}

		public bool Has(Reaction<T> reaction) => _reactions.Contains(reaction);
		public bool Has(Receiver<T> receiver) => _receivers.Contains(receiver);
		public bool Remove(Reaction<T> reaction) => _reactions.Remove(reaction);
		public bool Remove(Receiver<T> receiver) => _receivers.Remove(receiver);

		public bool Clear()
		{
			var cleared = _reactions.Count + _receivers.Count > 0;
			_reactions.Clear();
			_receivers.Clear();
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
		bool IEmitter.Add(IReaction reaction) => reaction is Reaction<T> casted && Add(casted);
		bool IEmitter.Add(IReceiver receiver) => receiver is Receiver<T> casted && Add(casted);
		bool IEmitter.Has(IReaction reaction) => reaction is Reaction<T> casted && Has(casted);
		bool IEmitter.Has(IReceiver receiver) => receiver is Receiver<T> casted && Has(casted);
		bool IEmitter.Remove(IReaction reaction) => reaction is Reaction<T> casted && Remove(casted);
		bool IEmitter.Remove(IReceiver receiver) => receiver is Receiver<T> casted && Remove(casted);
	}
}
