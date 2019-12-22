using Entia.Core;
using Entia.Core.Documentation;
using Entia.Messages;
using Entia.Modules.Component;
using System;
using System.Collections.Generic;

namespace Entia.Modules
{
    public sealed partial class Components
    {
        static Exception MissingComponent(Entity entity, Type type) =>
            new InvalidOperationException($"Missing component of type '{type.Format()}' on entity '{entity}'. Returning a dummy reference instead.");

        /// <summary>
        /// Gets a component of type <typeref name="T"/> associated with the entity <paramref name="entity"/>.
        /// If the component is missing, a <see cref="OnException"/> message will be emitted.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <param name="entity">The entity associated with the component.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>The component reference or <see cref="Dummy{T}.Value"/> if the component is missing.</returns>
        [ThreadSafe]
        public ref T Get<T>(Entity entity, States include = States.All) where T : struct, IComponent
        {
            if (TryStore<T>(entity, out var store, out var adjusted, include)) return ref store[adjusted];
            _onException.Emit(new OnException { Exception = MissingComponent(entity, typeof(T)) });
            return ref Dummy<T>.Value;
        }

        /// <summary>
        /// Gets a component of type <typeref name="T"/> associated with the <paramref name="entity"/>.
        /// If the component is missing, a dummy reference will be returned.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <param name="entity">The entity associated with the component.</param>
        /// <param name="success">Is <c>true</c> if the component was found; otherwise, <c>false</c>.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>The component reference or <see cref="Dummy{T}.Value"/> if the component is missing.</returns>
        [ThreadSafe]
        public ref T GetOrDummy<T>(Entity entity, out bool success, States include = States.All) where T : struct, IComponent
        {
            if (TryStore<T>(entity, out var store, out var adjusted, include))
            {
                success = true;
                return ref store[adjusted];
            }

            success = false;
            return ref Dummy<T>.Value;
        }

        /// <summary>
        /// Gets a component of type <typeref name="T"/> associated with the <paramref name="entity"/>.
        /// If the component is missing, a new instance will be created using the <paramref name="create"/> function.
        /// If the <paramref name="create"/> function is omitted, the default provider will be used.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <param name="entity">The entity associated with the component.</param>
        /// <param name="create">A function that creates a component of type <typeparamref name="T"/>.</param>
        /// <returns>The existing or added component reference.</returns>
        public ref T GetOrAdd<T>(Entity entity, Func<T> create = null) where T : struct, IComponent
        {
            if (TryStore<T>(entity, out var store, out var adjusted, States.All)) return ref store[adjusted];
            if (create == null) Set<T>(entity);
            else Set(entity, create());
            return ref Get<T>(entity, States.All);
        }

        /// <summary>
        /// Tries to get a component of type <typeparamref name="T"/> associated with the <paramref name="entity"/>.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="component">The component.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if the component was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool TryGet<T>(Entity entity, out T component, States include = States.All) where T : IComponent
        {
            if (ComponentUtility.Abstract<T>.TryConcrete(out var metadata))
                return TryGet(entity, metadata, out component, include);
            else if (ComponentUtility.TryGetConcreteTypes<T>(out var types))
                return TryGet(entity, types, out component, include);
            component = default;
            return false;
        }

        /// <summary>
        /// Tries to get a component of provided <paramref name="type"/> associated with the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="type">The component type.</param>
        /// <param name="component">The component.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if the component was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool TryGet(Entity entity, Type type, out IComponent component, States include = States.All)
        {
            if (ComponentUtility.TryGetMetadata(type, false, out var metadata))
                return TryGet(entity, metadata, out component, include);
            else if (ComponentUtility.TryGetConcreteTypes(type, out var types))
                return TryGet(entity, types, out component, include);
            component = default;
            return false;
        }

        /// <summary>
        /// Gets a component of provided <paramref name="type"/> associated with the entity <paramref name="entity"/>.
        /// If the component is missing, a <see cref="OnException"/> message will be emitted.
        /// </summary>
        /// <param name="entity">The entity associated with the component.</param>
        /// <param name="type">The component type.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>The component or null if the component is missing.</returns>
        [ThreadSafe]
        public IComponent Get(Entity entity, Type type, States include = States.All)
        {
            if (TryGet(entity, type, out var component, include)) return component;
            _onException.Emit(new OnException { Exception = MissingComponent(entity, type) });
            return null;
        }

        /// <summary>
        /// Gets all the components associated with the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity associated with the components.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>The components.</returns>
        public IEnumerable<IComponent> Get(Entity entity, States include = States.All)
        {
            ref var data = ref GetData(entity, out var success);
            return success ? Get<IComponent>(data, include) : Array.Empty<IComponent>();
        }

        /// <summary>
        /// Gets all components of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <returns>The components.</returns>
        [ThreadSafe]
        public IEnumerable<T> Get<T>(States include = States.All) where T : IComponent =>
            ComponentUtility.Abstract<T>.TryConcrete(out var metadata) ? Get<T>(metadata, include) :
            ComponentUtility.TryGetConcreteTypes<T>(out var types) ? Get<T>(types, include) :
            Array.Empty<T>();

        /// <summary>
        /// Gets all components of provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The component type.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>The components.</returns>
        [ThreadSafe]
        public IEnumerable<IComponent> Get(Type type, States include = States.All) =>
            ComponentUtility.TryGetMetadata(type, false, out var metadata) ? Get<IComponent>(metadata, include) :
            ComponentUtility.TryGetConcreteTypes(type, out var types) ? Get<IComponent>(types, include) :
            Array.Empty<IComponent>();

        /// <summary>
        /// Gets all components.
        /// </summary>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>The components.</returns>
        [ThreadSafe]
        public IEnumerable<IComponent> Get(States include = States.All)
        {
            foreach (var data in _data.Slice())
                if (data.IsValid)
                    foreach (var component in Get<IComponent>(data, include))
                        yield return component;
        }

        [ThreadSafe]
        IEnumerable<T> Get<T>(Data data, States include) where T : IComponent
        {
            var types = GetTargetTypes(data);
            foreach (var metadata in types)
            {
                if (TryGetStore(data, metadata, include, out var store, out var index))
                    yield return store is T[] casted ? casted[index] : (T)store.GetValue(index);
            }
        }

        [ThreadSafe]
        IEnumerable<T> Get<T>(Metadata[] types, States include = States.All) where T : IComponent
        {
            foreach (var type in types)
                foreach (var component in Get<T>(type, include))
                    yield return component;
        }

        [ThreadSafe]
        IEnumerable<T> Get<T>(Metadata metadata, States include = States.All) where T : IComponent
        {
            foreach (var data in _data.Slice())
            {
                if (data.IsValid && TryGetStore(data, metadata, include, out var store, out var index))
                    yield return store is T[] casted ? casted[index] : (T)store.GetValue(index);
            }
        }

        [ThreadSafe]
        bool TryGet<T>(Entity entity, in Metadata metadata, out T component, States include) where T : IComponent
        {
            ref readonly var data = ref GetData(entity, out var success);
            if (success && TryGet(data, metadata, out component, include)) return true;
            component = default;
            return false;
        }

        [ThreadSafe]
        bool TryGet<T>(in Data data, in Metadata metadata, out T component, States include) where T : IComponent
        {
            if (TryGetStore(data, metadata, include, out var store, out var index))
            {
                component = store is T[] casted ? casted[index] : (T)store.GetValue(index);
                return true;
            }

            component = default;
            return false;
        }

        [ThreadSafe]
        bool TryGet<T>(Entity entity, Metadata[] types, out T component, States include) where T : IComponent
        {
            ref readonly var data = ref GetData(entity, out var success);
            if (success && TryGet(data, types, out component, include)) return true;
            component = default;
            return false;
        }

        [ThreadSafe]
        bool TryGet<T>(in Data data, Metadata[] types, out T component, States include) where T : IComponent
        {
            for (int i = 0; i < types.Length; i++) if (TryGet(data, types[i], out component, include)) return true;
            component = default;
            return false;
        }
    }
}