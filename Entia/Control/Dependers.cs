using System.Collections.Generic;
using System.Reflection;
using Entia.Dependencies;
using Entia.Modules;

namespace Entia.Dependers
{
    public sealed class React<T> : IDepender
    {
        public IEnumerable<IDependency> Depend(MemberInfo member, World world)
        {
            yield return new React(typeof(T));
            foreach (var dependency in world.Dependers().Dependencies<T>()) yield return dependency;
        }
    }
}