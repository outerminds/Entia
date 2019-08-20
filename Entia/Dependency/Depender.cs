using Entia.Core;
using Entia.Dependencies;
using Entia.Dependency;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Dependers
{
    public interface IDepender : ITrait, IImplementation<object, Default>
    {
        IEnumerable<IDependency> Depend(in Context context);
    }

    public sealed class Default : IDepender
    {
        public IEnumerable<IDependency> Depend(in Context context) => Array.Empty<IDependency>();
    }

    public sealed class Fields : IDepender
    {
        public IEnumerable<IDependency> Depend(in Context context) =>
            context.Member is Type type ? type.InstanceFields().SelectMany(context.Dependencies) :
            Array.Empty<IDependency>();
    }

    public sealed class Provider : IDepender
    {
        readonly Func<Context, IEnumerable<IDependency>> _provider;
        public Provider(Func<Context, IEnumerable<IDependency>> provider) { _provider = provider; }
        public IEnumerable<IDependency> Depend(in Context context) => _provider(context);
    }

    public static class Depender
    {
        public static IDepender From(Func<Context, IEnumerable<IDependency>> provider) => new Provider(provider);
        public static IDepender From(Func<IEnumerable<IDependency>> provider) => new Provider(_ => provider());
        public static IDepender From(params IDependency[] dependencies) => new Provider(_ => dependencies);
        public static IDepender From<T>(params IDependency[] dependencies) => new Provider(context => dependencies.Concat(context.Dependencies<T>()));
    }
}
