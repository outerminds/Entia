using System.Collections.Generic;
using Entia.Core;
using Entia.Dependencies;
using Entia.Dependency;

namespace Entia.Dependers
{
    public sealed class React<T> : IDepender where T : struct, IMessage
    {
        public IEnumerable<IDependency> Depend(in Context context) => context.Dependencies<T>()
            .Prepend(new React(typeof(T)));
    }

    public sealed class React<TMessage, T> : IDepender where TMessage : struct, IMessage where T : struct, IComponent
    {
        public IEnumerable<IDependency> Depend(in Context context) => context.Dependencies<TMessage>()
            .Concat(context.Dependencies<T>())
            .Prepend(new React(typeof(TMessage)), new Write(typeof(T)));
    }
}