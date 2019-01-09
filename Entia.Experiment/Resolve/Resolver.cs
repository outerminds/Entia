using System;
using Entia.Core;
using Entia.Experiment.Resolvables;

namespace Entia.Experiment.Resolvers
{
    public interface IResolver { }
    public interface IResolver<T> : IResolver where T : struct, IResolvablez
    {
        void Resolve(in T resolvable);
    }

    delegate void Resolve<T>(in T resolvable);

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ResolverAttribute : PreserveAttribute { }

    public sealed class Default<T> : IResolver<T> where T : struct, IResolvablez
    {
        public void Resolve(in T resolvable) { throw new NotImplementedException(); }
    }
}