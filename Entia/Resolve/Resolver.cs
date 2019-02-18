using System;
using Entia.Core;
using Entia.Resolvables;

namespace Entia.Resolvers
{
    public interface IResolver
    {
        bool Resolve(IResolvable resolvable);
    }

    public abstract class Resolver<T> : IResolver where T : struct, IResolvable
    {
        public abstract bool Resolve(in T resolvable);
        bool IResolver.Resolve(IResolvable resolvable) => resolvable is T casted && Resolve(casted);
    }

    [AttributeUsage(Modules.ModuleUtility.AttributeUsage)]
    public sealed class ResolverAttribute : PreserveAttribute { }

    public sealed class Default<T> : Resolver<T> where T : struct, IResolvable
    {
        public override bool Resolve(in T resolvable) => false;
    }
}