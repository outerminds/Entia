using System.Collections.Generic;
using System.Linq;
using Entia.Dependencies;
using Entia.Dependency;

namespace Entia.Dependers
{
    public sealed class React<T> : IDepender
    {
        public IEnumerable<IDependency> Depend(in Context context) =>
            context.Dependencies<T>().Prepend(new React(typeof(T)));
    }
}