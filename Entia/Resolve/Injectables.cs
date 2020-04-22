using System;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Injectors;
using Entia.Modules;

namespace Entia.Injectables
{
    [ThreadSafe]
    public readonly struct Defer : IInjectable
    {
        [ThreadSafe]
        public readonly struct Entities : IInjectable
        {
            [Implementation]
            static Injector<Entities> _injector => Injector.From(context =>
                new Entities(context.World.Resolvers(), context.World.Entities()));

            readonly Modules.Resolvers _resolvers;
            readonly Modules.Entities _entities;

            public Entities(Modules.Resolvers resolvers, Modules.Entities entities)
            {
                _resolvers = resolvers;
                _entities = entities;
            }

            public bool Create() =>
                _resolvers.Defer(_entities, state => state.Create());
            public bool Create(Action<Entity> initialize) =>
                _resolvers.Defer((initialize, _entities), current => current.initialize(current._entities.Create()));
            public bool Create<T>(in T state, Action<Entity, T> initialize) =>
                _resolvers.Defer((state, initialize, _entities), current => current.initialize(current._entities.Create(), current.state));

            public bool Destroy(Entity entity) =>
                _resolvers.Defer((entity, _entities), state => state._entities.Destroy(state.entity));

            public bool Clear() =>
                _resolvers.Defer(_entities, state => state.Clear());
        }

        [ThreadSafe]
        public readonly struct Components : IInjectable
        {
            [Implementation]
            static Injector<Components> _injector => Injector.From(context =>
                new Components(context.World.Resolvers(), context.World.Components()));

            readonly Modules.Resolvers _resolvers;
            readonly Modules.Components _components;

            public Components(Modules.Resolvers resolvers, Modules.Components components)
            {
                _resolvers = resolvers;
                _components = components;
            }

            public bool Set<T>(Entity entity) where T : struct, IComponent =>
                _resolvers.Defer((entity, _components), state => state._components.Set<T>(state.entity));
            public bool Set<T>(Entity entity, in T component) where T : struct, IComponent =>
                _resolvers.Defer((entity, component, _components), state => state._components.Set(state.entity, state.component));
            public bool Set(Entity entity, Type type) =>
                _resolvers.Defer((entity, type, _components), state => state._components.Set(state.entity, state.type));
            public bool Set(Entity entity, IComponent component) =>
                _resolvers.Defer((entity, component, _components), state => state._components.Set(state.entity, state.component));

            public bool Remove<T>(Entity entity, States include = States.All) where T : IComponent =>
                _resolvers.Defer((entity, include, _components), state => state._components.Remove<T>(state.entity, state.include));
            public bool Remove(Entity entity, Type type, States include = States.All) =>
                _resolvers.Defer((entity, type, include, _components), state => state._components.Remove(state.entity, state.type, state.include));

            public bool Clear<T>(States include = States.All) where T : IComponent =>
                _resolvers.Defer((include, _components), state => state._components.Clear<T>(state.include));
            public bool Clear(Type type, States include = States.All) =>
                _resolvers.Defer((type, include, _components), state => state._components.Clear(state.type, state.include));
            public bool Clear(Entity entity, States include = States.All) =>
                _resolvers.Defer((entity, include, _components), state => state._components.Clear(state.entity, state.include));
            public bool Clear(States include = States.All) =>
                _resolvers.Defer((include, _components), state => state._components.Clear(state.include));
        }

        [Implementation]
        static Injector<Defer> _injector => Injector.From(context => new Defer(context.World.Resolvers()));

        readonly Modules.Resolvers _resolvers;

        public Defer(Modules.Resolvers resolvers) { _resolvers = resolvers; }

        public bool Do(Action action) => _resolvers.Defer(action);
        public bool Do(Action<World> action) => _resolvers.Defer(action);
        public bool Do<T>(in T state, Action<T> action) => _resolvers.Defer(state, action);
        public bool Do<T>(in T state, Action<T, World> action) => _resolvers.Defer(state, action);
    }
}