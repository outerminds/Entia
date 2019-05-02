using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Entia.Core;
using Entia.Injectors;
using Entia.Modules;
using Entia.Resolvables;

namespace Entia.Injectables
{
    public readonly struct Defer : IInjectable
    {
        [Injector]
        static Injector<Defer> Injector => Injectors.Injector.From(world => new Defer(world.Resolvers()));

        readonly Modules.Resolvers _resolvers;

        public Defer(Modules.Resolvers resolvers) { _resolvers = resolvers; }

        public void Do<T>(T state, Action<T> action) => _resolvers.Defer(new Do<T>(state, action));
    }
}