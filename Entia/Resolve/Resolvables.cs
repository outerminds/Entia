using System;
using System.Runtime.CompilerServices;
using Entia.Core;
using Entia.Resolvers;
using Entia.Modules;

namespace Entia.Resolvables
{
    public interface IResolvable { }
    public interface IResolvable<T> : IResolvable where T : IResolver, new() { }

    public readonly struct Do<T> : IResolvable<Do<T>.Resolver>
    {
        sealed class Resolver : Resolver<Do<T>>
        {
            public override bool Resolve(in Do<T> resolvable)
            {
                resolvable.Action(resolvable.State);
                return true;
            }
        }

        public readonly T State;
        public readonly Action<T> Action;

        public Do(T state, Action<T> action)
        {
            State = state;
            Action = action;
        }
    }
}