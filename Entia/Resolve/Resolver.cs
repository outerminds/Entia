using System;
using Entia.Core;
using Entia.Resolvables;

namespace Entia.Resolvers
{
    public interface IResolver
    {
        void Resolve(IResolvable resolvable);
    }

    public abstract class Resolver<T> : IResolver where T : struct, IResolvable
    {
        public abstract void Resolve(in T resolvable);

        void IResolver.Resolve(IResolvable resolvable)
        {
            if (resolvable is T casted) Resolve(casted);
        }
    }

    [AttributeUsage(Modules.ModuleUtility.AttributeUsage)]
    public sealed class ResolverAttribute : PreserveAttribute { }

    public sealed class Default<T> : Resolver<T> where T : struct, IResolvable
    {
        public override void Resolve(in T resolvable) { throw new NotImplementedException(); }
    }
}