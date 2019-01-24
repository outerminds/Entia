using Entia.Core;
using Entia.Core.Documentation;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Modules.Message
{
    public interface IReaction
    {
        Type Type { get; }
        int Count { get; }
        bool Has(Delegate reaction);
        bool Add(Delegate reaction);
        bool Remove(Delegate reaction);
        bool Clear();
    }

    public sealed class Reaction<T> : IReaction, IEnumerable<Delegate>
        where T : struct, IMessage
    {
        static readonly InAction<T> _empty = (in T _) => { };

        [ThreadSafe]
        public int Count => _reactions.Count;

        Type IReaction.Type => typeof(T);

        event InAction<T> _reaction = _empty;
        readonly Dictionary<Delegate, InAction<T>> _reactions = new Dictionary<Delegate, InAction<T>>();

        [ThreadSafe]
        public bool Has(Action reaction) => Has(reaction as Delegate);
        [ThreadSafe]
        public bool Has(InAction<T> reaction) => Has(reaction as Delegate);

        public bool Add(Action reaction)
        {
            if (Has(reaction)) return false;
            InAction<T> wrapped = (in T _) => reaction();
            _reaction += wrapped;
            _reactions[reaction] = wrapped;
            return true;
        }

        public bool Add(InAction<T> reaction)
        {
            if (Has(reaction)) return false;
            _reaction += reaction;
            _reactions[reaction] = reaction;
            return true;
        }

        public bool Remove(Action reaction) => Remove(reaction as Delegate);
        public bool Remove(InAction<T> reaction) => Remove(reaction as Delegate);

        public bool Clear()
        {
            var cleared = _reactions.Count > 0;
            _reaction = _empty;
            _reactions.Clear();
            return cleared;
        }

        [ThreadSafe]
        public void React(in T message) => _reaction(message);

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<Delegate> GetEnumerator() => _reactions.Keys.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [ThreadSafe]
        bool Has(Delegate reaction) => _reactions.ContainsKey(reaction);

        bool Remove(Delegate reaction)
        {
            if (_reactions.TryGetValue(reaction, out var wrapped))
            {
                _reaction -= wrapped;
                _reactions.Remove(reaction);
                return true;
            }

            return false;
        }

        [ThreadSafe]
        bool IReaction.Has(Delegate reaction) =>
            reaction is Action action ? Has(action) : reaction is InAction<T> actionT ? Has(actionT) : false;
        bool IReaction.Add(Delegate reaction) =>
            reaction is Action action ? Add(action) : reaction is InAction<T> actionT ? Add(actionT) : false;
        bool IReaction.Remove(Delegate reaction) =>
            reaction is Action action ? Remove(action) : reaction is InAction<T> actionT ? Remove(actionT) : false;
    }
}
