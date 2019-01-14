using Entia.Core;
using Entia.Dependables;
using Entia.Injectors;
using Entia.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Injectables
{
    public readonly struct AllComponents : IInjectable, IEnumerable<IComponent>,
        IDepend<Dependencies.Unknown, Read<Entity>, Emit<Messages.OnAdd>, Emit<Messages.OnRemove>>
    {
        sealed class Injector : Injector<AllComponents>
        {
            public override Result<AllComponents> Inject(MemberInfo member, World world) => new AllComponents(world.Components());
        }

        [Injector]
        static readonly Injector _injector = new Injector();

        readonly Components _components;

        public AllComponents(Components components) { _components = components; }

        public ref readonly T Read<T>(Entity entity) where T : struct, IComponent => ref _components.Get<T>(entity);
        public ref T Get<T>(Entity entity) where T : struct, IComponent => ref _components.Get<T>(entity);
        public ref T GetOrAdd<T>(Entity entity, Func<T> create = null) where T : struct, IComponent => ref _components.GetOrAdd(entity, create);
        public ref T GetOrDummy<T>(Entity entity, out bool success) where T : struct, IComponent => ref _components.GetOrDummy<T>(entity, out success);
        public bool TryGet<T>(Entity entity, out T component) where T : struct, IComponent => _components.TryGet(entity, out component);
        public bool TryGet(Entity entity, Type type, out IComponent component) => _components.TryGet(entity, type, out component);
        public IEnumerable<IComponent> Get(Entity entity) => _components.Get(entity);
        public IEnumerable<(Entity entity, T component)> Get<T>() where T : struct, IComponent => _components.Get<T>();
        public IEnumerable<(Entity entity, IComponent component)> Get(Type component) => _components.Get(component);
        public bool Set<T>(Entity entity, in T component) where T : struct, IComponent => _components.Set(entity, component);
        public bool Set(Entity entity, IComponent component) => _components.Set(entity, component);
        public bool Has<T>(Entity entity) where T : struct, IComponent => _components.Has<T>(entity);
        public bool Has(Entity entity, Type component) => _components.Has(entity, component);
        public bool Remove<T>(Entity entity) where T : struct, IComponent => _components.Remove<T>(entity);
        public bool Remove(Entity entity, Type component) => _components.Remove(entity, component);
        public bool Clear<T>() where T : struct, IComponent => _components.Clear<T>();
        public bool Clear(Type component) => _components.Clear(component);
        public bool Clear(Entity entity) => _components.Clear(entity);
        public bool Clear() => _components.Clear();
        public IEnumerator<IComponent> GetEnumerator() => _components.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _components.GetEnumerator();
    }

    public readonly struct Components<T> : IInjectable, IEnumerable<(Entity entity, T component)>,
        IDepend<Read<Entity>, Write<T>, Emit<Messages.OnAdd>, Emit<Messages.OnRemove>, Emit<Messages.OnAdd<T>>, Emit<Messages.OnRemove<T>>>
        where T : struct, IComponent
    {
        public readonly struct Read : IInjectable, IDepend<Read<Entity>, Read<T>>, IEnumerable<(Entity entity, T component)>
        {
            sealed class Injector : Injector<Read>
            {
                public override Result<Read> Inject(MemberInfo member, World world) => new Read(world.Components());
            }

            [Injector]
            static readonly Injector _injector = new Injector();

            readonly Components _components;

            public Read(Components components) { _components = components; }

            public ref readonly T GetOrAdd(Entity entity, Func<T> create = null) => ref _components.GetOrAdd(entity, create);
            public ref readonly T GetOrDummy(Entity entity, out bool success) => ref _components.GetOrDummy<T>(entity, out success);
            public bool TryGet(Entity entity, out T component) => _components.TryGet(entity, out component);
            public ref readonly T Get(Entity entity) => ref _components.Get<T>(entity);
            public bool Has(Entity entity) => _components.Has<T>(entity);
            public IEnumerator<(Entity entity, T component)> GetEnumerator() => _components.Get<T>().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        sealed class Injector : Injector<Components<T>>
        {
            public override Result<Components<T>> Inject(MemberInfo member, World world) => new Components<T>(world.Components());
        }

        [Injector]
        static readonly Injector _injector = new Injector();

        public static implicit operator Read(Components<T> components) => new Read(components._components);

        readonly Components _components;

        public Components(Components components) { _components = components; }

        public ref T GetOrAdd(Entity entity, Func<T> create = null) => ref _components.GetOrAdd(entity, create);
        public ref T GetOrDummy(Entity entity, out bool success) => ref _components.GetOrDummy<T>(entity, out success);
        public bool TryGet(Entity entity, out T component) => _components.TryGet(entity, out component);
        public ref T Get(Entity entity) => ref _components.Get<T>(entity);
        public bool Set(Entity entity, in T component) => _components.Set(entity, component);
        public bool Has(Entity entity) => _components.Has<T>(entity);
        public bool Remove(Entity entity) => _components.Remove<T>(entity);
        public bool Clear() => _components.Clear<T>();
        public IEnumerator<(Entity entity, T component)> GetEnumerator() => _components.Get<T>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
