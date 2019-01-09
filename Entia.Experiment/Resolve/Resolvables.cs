using System;
using System.Runtime.CompilerServices;
using Entia.Core;
using Entia.Experiment.Resolvers;
using Entia.Modules;

namespace Entia.Experiment.Resolvables
{
    public interface IResolvablez { }
    public interface IResolvablez<T> where T : IResolver, new() { }

    public readonly struct Do<T> : IResolvablez
    {
        sealed class Resolver : IResolver<Do<T>>
        {
            public void Resolve(in Do<T> resolvable) => resolvable.Action(resolvable.State);
        }

        [Resolver]
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