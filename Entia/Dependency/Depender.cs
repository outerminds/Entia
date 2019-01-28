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

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class DependerAttribute : PreserveAttribute { }

    public sealed class Default : IDepender
    {
        public IEnumerable<IDependency> Depend(MemberInfo member, World world) => Array.Empty<IDependency>();
    }

    public sealed class Fields : IDepender
    {
        public IEnumerable<IDependency> Depend(MemberInfo member, World world) => member is IReflect reflect ?
            reflect.GetFields(TypeUtility.Instance).SelectMany(world.Dependers().Dependencies) :
            Array.Empty<IDependency>();
    }
}
