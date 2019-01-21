using Entia.Core;
using Entia.Dependencies;
using Entia.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Entia.Dependers
{
    public sealed class System : IDepender
    {
        public IEnumerable<IDependency> Depend(MemberInfo member, World world) => member is IReflect reflect ?
            reflect.GetMembers(TypeUtility.Instance).SelectMany(world.Dependers().Dependencies) :
            Array.Empty<IDependency>();
    }
}
