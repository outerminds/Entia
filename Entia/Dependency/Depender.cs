using Entia.Core;
using Entia.Dependencies;
using Entia.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Entia.Dependers
{
    public interface IDepender
    {
        IEnumerable<IDependency> Depend(MemberInfo member, World world);
    }

    [AttributeUsage(ModuleUtility.AttributeUsage)]
    public sealed class DependerAttribute : PreserveAttribute { }

    public sealed class Default : IDepender
    {
        public IEnumerable<IDependency> Depend(MemberInfo member, World world) => Array.Empty<IDependency>();
    }

    public sealed class Fields : IDepender
    {
        public IEnumerable<IDependency> Depend(MemberInfo member, World world) =>
            member is Type type ? type.InstanceFields().SelectMany(world.Dependers().Dependencies) :
            Array.Empty<IDependency>();
    }

    public sealed class Provider : IDepender
    {
        readonly Func<World, IEnumerable<IDependency>> _provider;
        public Provider(Func<World, IEnumerable<IDependency>> provider) { _provider = provider; }
        public IEnumerable<IDependency> Depend(MemberInfo member, World world) => _provider(world);
    }

    public static class Depender
    {
        public static IDepender From(Func<World, IEnumerable<IDependency>> dependencies) => new Provider(dependencies);
        public static IDepender From(Func<IEnumerable<IDependency>> dependencies) => new Provider(_ => dependencies());
        public static IDepender From(params IDependency[] dependencies) => new Provider(_ => dependencies);
        public static IDepender From<T>(params IDependency[] dependencies) => new Provider(world => dependencies.Concat(world.Dependers().Dependencies<T>()));
    }
}
