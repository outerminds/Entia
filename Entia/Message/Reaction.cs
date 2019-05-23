using Entia.Core;
using Entia.Core.Documentation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Entia.Modules.Message
{
    [ThreadSafe]
    public interface IReaction : IEnumerable<Delegate>
    {
        Type Type { get; }
        Delegate React { get; }
        bool Add(Delegate reaction);
        bool Remove(Delegate reaction);
        bool Clear();
    }

    [ThreadSafe]
    public sealed class Reaction<T> : IReaction where T : struct, IMessage
    {
        static readonly InAction<T> _empty = (in T _) => { };

        public InAction<T> React => _reaction;

        Delegate IReaction.React => _reaction;
        Type IReaction.Type => typeof(T);

        InAction<T> _reaction = _empty;

        public bool Add(InAction<T> reaction) => _reaction != Concurrent.Mutate(ref _reaction, reaction, (previous, current) => previous + current);
        public bool Remove(InAction<T> reaction) => _reaction != Concurrent.Mutate(ref _reaction, reaction, (previous, current) => previous - current);
        public bool Clear() => _reaction != Concurrent.Mutate(ref _reaction, _ => _empty);

        public IEnumerator<Delegate> GetEnumerator() => _reaction.GetInvocationList().Slice().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        bool IReaction.Add(Delegate reaction) => reaction is InAction<T> action && Add(action);
        bool IReaction.Remove(Delegate reaction) => reaction is InAction<T> action && Remove(action);
    }
}
