using System;
using Entia.Core;
using Entia.Resolvers;

namespace Entia.Resolvables
{
    public interface IResolvable { }

    public readonly struct Do<T> : IResolvable
    {
        sealed class Resolver : IResolver<Do<T>>
        {
            public bool Resolve(in Do<T> resolvable)
            {
                resolvable.Action(resolvable.State);
                return true;
            }
        }

        [Implementation]
        static readonly Resolver _resolver = new Resolver();

        public readonly T State;
        public readonly Action<T> Action;

        public Do(T state, Action<T> action)
        {
            State = state;
            Action = action;
        }
    }
}