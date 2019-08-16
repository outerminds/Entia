using System;
using Entia.Core;
using Entia.Resolvables;

namespace Entia.Resolvers
{
    public interface IResolver<T> : ITrait where T : struct, IResolvable
    {
        bool Resolve(in T resolvable);
    }
}