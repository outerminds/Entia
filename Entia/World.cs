using Entia.Core;
using Entia.Dependables;
using Entia.Injectables;
using Entia.Injectors;
using Entia.Modules;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Entia
{
    public sealed class World : IInjectable, IDepend<Dependencies.Unknown>, IEnumerable<IModule>
    {
        sealed class Injector : Injector<World>
        {
            public override Result<World> Inject(MemberInfo member, World world) => world;
        }

        [Injector]
        static readonly Injector _injector = new Injector();

        readonly TypeMap<IModule, IModule> _modules = new TypeMap<IModule, IModule>();

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
        public bool Set<T>(T module) where T : IModule => _modules.Set<T>(module);
        public bool Has<T>() where T : IModule => _modules.Has<T>();
        public bool Remove<T>() where T : IModule => _modules.Remove<T>();
        public bool Clear() => _modules.Clear();
        public void Resolve()
        {
            foreach (var module in _modules.Values) (module as IResolvable)?.Resolve();
        }

        public TypeMap<IModule, IModule>.ValueEnumerator GetEnumerator() => _modules.Values.GetEnumerator();
        IEnumerator<IModule> IEnumerable<IModule>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
