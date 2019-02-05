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
        sealed class Injector : Injector<World>
        {
            public override Result<World> Inject(MemberInfo member, World world) => world;
        }

        [Depender]
        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                yield return new Dependencies.Unknown();
            }
        }

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
        public void Resolve()
        {
            for (int i = 0; i < _resolvables.Length; i++) _resolvables[i].Resolve();
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public TypeMap<IModule, IModule>.ValueEnumerator GetEnumerator() => _modules.Values.GetEnumerator();
        IEnumerator<IModule> IEnumerable<IModule>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
