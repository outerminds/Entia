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
        public readonly struct Entities : IInjectable
        {
            [Implementation]
            static Injector<Entities> Injector => Injectors.Injector.From(context =>
                new Entities(context.World.Resolvers(), context.World.Entities()));

            readonly Modules.Resolvers _resolvers;
            readonly Modules.Entities _entities;

            public Entities(Modules.Resolvers resolvers, Modules.Entities entities)
            {
                _resolvers = resolvers;
                _entities = entities;
            }

            public void Create() =>
                _resolvers.Defer(_entities, state => state.Create());
            public void Create(Action<Entity> initialize) =>
                _resolvers.Defer((initialize, _entities), current => current.initialize(current._entities.Create()));
            public void Create<T>(in T state, Action<Entity, T> initialize) =>
                _resolvers.Defer((state, initialize, _entities), current => current.initialize(current._entities.Create(), current.state));

            public void Destroy(Entity entity) =>
                _resolvers.Defer((entity, _entities), state => state._entities.Destroy(state.entity));

            public void Clear() =>
                _resolvers.Defer(_entities, state => state.Clear());
        }

        public readonly struct Components : IInjectable
        {
            [Implementation]
            static Injector<Components> Injector => Injectors.Injector.From(context => new Components(context.World.Resolvers(), context.World.Components()));

            readonly Modules.Resolvers _resolvers;
            readonly Modules.Components _components;

            public Components(Modules.Resolvers resolvers, Modules.Components components)
            {
                _resolvers = resolvers;
                _components = components;
            }

            public void Set<T>(Entity entity) where T : struct, IComponent =>
                _resolvers.Defer((entity, _components), state => state._components.Set<T>(state.entity));
            public void Set<T>(Entity entity, in T component) where T : struct, IComponent =>
                _resolvers.Defer((entity, component, _components), state => state._components.Set(state.entity, state.component));
            public void Set(Entity entity, Type type) =>
                _resolvers.Defer((entity, type, _components), state => state._components.Set(state.entity, state.type));
            public void Set(Entity entity, IComponent component) =>
                _resolvers.Defer((entity, component, _components), state => state._components.Set(state.entity, state.component));

            public void Remove<T>(Entity entity, States include = States.All) where T : IComponent =>
                _resolvers.Defer((entity, include, _components), state => state._components.Remove<T>(state.entity, state.include));
            public void Remove(Entity entity, Type type, States include = States.All) =>
                _resolvers.Defer((entity, type, include, _components), state => state._components.Remove(state.entity, state.type, state.include));

            public void Clear<T>(States include = States.All) where T : IComponent =>
                _resolvers.Defer((include, _components), state => state._components.Clear<T>(state.include));
            public void Clear(Type type, States include = States.All) =>
                _resolvers.Defer((type, include, _components), state => state._components.Clear(state.type, state.include));
            public void Clear(Entity entity, States include = States.All) =>
                _resolvers.Defer((entity, include, _components), state => state._components.Clear(state.entity, state.include));
            public void Clear(States include = States.All) =>
                _resolvers.Defer((include, _components), state => state._components.Clear(state.include));
        }

        [Implementation]
        static Injector<Defer> Injector => Injectors.Injector.From(context => new Defer(context.World.Resolvers()));

        readonly Modules.Resolvers _resolvers;

        public Defer(Modules.Resolvers resolvers) { _resolvers = resolvers; }

        public void Do(Action action) => _resolvers.Defer(action);
        public void Do(Action<World> action) => _resolvers.Defer(action);
        public void Do<T>(in T state, Action<T> action) => _resolvers.Defer(state, action);
        public void Do<T>(in T state, Action<T, World> action) => _resolvers.Defer(state, action);
    }
}