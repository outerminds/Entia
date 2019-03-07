using Entia.Core;
using Entia.Injectables;
using Entia.Injectors;
using Entia.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Entia.Modules
{
    public sealed class Injectors : IModule, IEnumerable<IInjector>
    {
        readonly World _world;
        readonly TypeMap<IInjectable, IInjector> _defaults = new TypeMap<IInjectable, IInjector>();
        readonly TypeMap<IInjectable, IInjector> _injectors = new TypeMap<IInjectable, IInjector>();

        public Injectors(World world) { _world = world; }

        public Result<T> Inject<T>(MemberInfo member = null) where T : IInjectable => Get<T>().Inject(member ?? typeof(T), _world).Cast<T>();
        public Result<object> Inject(MemberInfo member)
        {
            var type = member as Type ??
                (member as FieldInfo)?.FieldType ??
                (member as PropertyInfo)?.PropertyType;
            if (type == null) return Result.Failure($"Expected member '{member}' to be a '{typeof(Type).Format()}', '{typeof(FieldInfo).Format()}' or '{typeof(PropertyInfo).Format()}'.");
            return Inject(type, member);
        }
        public Result<object> Inject(Type injectable, MemberInfo member = null) =>
            Get(injectable).Inject(member ?? injectable, _world).As(injectable);

        public IInjector Default<T>() where T : IInjectable => Default(typeof(T));
        public IInjector Default(Type injectable) => _defaults.Default(injectable, typeof(IInjectable<>), typeof(InjectorAttribute), _ => new Default());
        public bool Has<T>() where T : IInjectable => _injectors.Has<T>(true);
        public bool Has(Type injectable) => _injectors.Has(injectable, true);
        public IInjector Get<T>() where T : IInjectable => _injectors.TryGet<T>(out var injector, true) ? injector : Default<T>();
        public IInjector Get(Type injectable) => _injectors.TryGet(injectable, out var injector, true) ? injector : Default(injectable);
        public bool Set<T>(Injector<T> injector) where T : IInjectable => _injectors.Set<T>(injector);
        public bool Set(Type injectable, IInjector injector) => _injectors.Set(injectable, injector);
        public bool Remove<T>() where T : IInjectable => _injectors.Remove<T>();
        public bool Remove(Type injectable) => _injectors.Remove(injectable);
        public bool Clear() => _defaults.Clear() | _injectors.Clear();
        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<IInjector> GetEnumerator() => _injectors.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
