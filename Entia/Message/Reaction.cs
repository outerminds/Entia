using Entia.Core;
using Entia.Core.Documentation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Entia.Modules.Message
{
    public interface IReaction : IEnumerable<Delegate>
    {
        Type Type { get; }
        bool Add(Delegate reaction);
        bool Remove(Delegate reaction);
        bool Clear();
    }

    [ThreadSafe]
    public sealed class Reaction<T> : IReaction where T : struct, IMessage
    {
        static readonly InAction<T> _empty = (in T _) => { };

        public InAction<T> React => _reaction;

        Type IReaction.Type => typeof(T);

        event InAction<T> _reaction = _empty;

        public void Add(InAction<T> reaction) => _reaction += reaction;
        public void Remove(InAction<T> reaction) => _reaction -= reaction;
        public bool Clear()
        {
            var initial = _reaction;
            var current = initial;
            var comparand = current;
            do
            {
                comparand = current;
                current = Interlocked.CompareExchange(ref _reaction, _empty, comparand);
            }
            while (current != comparand);
            return initial != current;
        }

        bool IReaction.Add(Delegate reaction)
        {
            if (reaction is InAction<T> action)
            {
                Add(action);
                return true;
            }

            return false;
        }

        bool IReaction.Remove(Delegate reaction)
        {
            if (reaction is InAction<T> action)
            {
                Remove(action);
                return true;
            }
            return false;
        }

        public IEnumerator<Delegate> GetEnumerator() => _reaction.GetInvocationList().Slice().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
