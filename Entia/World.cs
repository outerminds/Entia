using Entia.Core;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Injectables;
using Entia.Injectors;
using Entia.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Entia
{
    public sealed class World : IInjectable, IEnumerable<IModule>
    {
        [Injector]
        static readonly Injector<World> _injector = Injector.From(world => world);
        [Depender]
        static readonly IDepender _depender = Depender.From(new Dependencies.Unknown());

        readonly TypeMap<IModule, IModule> _modules = new TypeMap<IModule, IModule>();
        IResolvable[] _resolvables = Array.Empty<IResolvable>();

        public bool TryGet<T>(out T module) where T : IModule
        {
            if (_modules.TryGet<T>(out var value) && value is T casted)
            {
                module = casted;
                return true;
            }

            module = default;
            return false;
        }
        public bool Set<T>(T module) where T : IModule
        {
            Remove<T>();
            if (module is IResolvable resolvable) ArrayUtility.Add(ref _resolvables, resolvable);
            return _modules.Set<T>(module);
        }
        public bool Has<T>() where T : IModule => _modules.Has<T>();
        public bool Remove<T>() where T : IModule
        {
            if (_modules.TryGet<T>(out var module) && module is IResolvable resolvable) ArrayUtility.Remove(ref _resolvables, resolvable);
            return _modules.Remove<T>();
        }
        public bool Clear()
        {
            _resolvables = Array.Empty<IResolvable>();
            return _modules.Clear();
        }
        public bool Resolve()
        {
            var resolved = false;
            for (int i = 0; i < _resolvables.Length; i++) resolved |= _resolvables[i].Resolve();
            return resolved;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public TypeMap<IModule, IModule>.ValueEnumerator GetEnumerator() => _modules.Values.GetEnumerator();
        IEnumerator<IModule> IEnumerable<IModule>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
