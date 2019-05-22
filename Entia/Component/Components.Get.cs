using Entia.Components;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Messages;
using Entia.Modules.Component;
using Entia.Modules.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    /// <summary>
    /// Module that stores and manages components.
    /// </summary>
    public sealed partial class Components : IModule, IResolvable, IEnumerable<IComponent>
    {
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
            _onException.Emit(new OnException { Exception = ExceptionUtility.MissingComponent(entity, typeof(T)) });
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
        public bool TryGet<T>(Entity entity, out T component, States include = States.All) where T : struct, IComponent
        {
            component = GetOrDummy<T>(entity, out var success, include);
            return success;
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
            if (ComponentUtility.TryGetMetadata(type, false, out var metadata)) return TryGet(entity, metadata, out component, include);
            else if (ComponentUtility.TryGetConcreteTypes(type, out var types)) return TryGet(entity, types, out component, include);
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
            _onException.Emit(new OnException { Exception = ExceptionUtility.MissingComponent(entity, type) });
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
            return success ? Get(data, include) : Array.Empty<IComponent>();
        }

        /// <summary>
        /// Gets all entity-component pairs that have a component of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <returns>The entity-component pairs.</returns>
        [ThreadSafe]
        public IEnumerable<(Entity entity, T component)> Get<T>(States include = States.All) where T : struct, IComponent
        {
            if (ComponentUtility.TryGetMetadata<T>(false, out var metadata))
            {
                foreach (var data in _data.Slice())
                {
                    if (data.IsValid && TryGetStore<T>(data, metadata, include, out var store, out var index))
                        yield return (data.Segment.Entities.items[data.Index], store[index]);
                }
            }
        }

        /// <summary>
        /// Gets all entity-component pairs that have a component of provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The component type.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>The entity-component pairs.</returns>
        [ThreadSafe]
        public IEnumerable<(Entity entity, IComponent component)> Get(Type type, States include = States.All) =>
            ComponentUtility.TryGetMetadata(type, false, out var metadata) ? Get(metadata, include) :
            ComponentUtility.TryGetConcreteTypes(type, out var types) ? Get(types, include) :
            Array.Empty<(Entity, IComponent)>();

        [ThreadSafe]
        IEnumerable<IComponent> Get(Data data, States include)
        {
            var types = GetTargetTypes(data);
            foreach (var metadata in types)
            {
                if (TryGetStore(data, metadata, include, out var store, out var index))
                    yield return (IComponent)store.GetValue(index);
            }
        }

        [ThreadSafe]
        IEnumerable<(Entity entity, IComponent component)> Get(Metadata[] types, States include = States.All)
        {
            foreach (var type in types) foreach (var pair in Get(type, include)) yield return pair;
        }

        [ThreadSafe]
        IEnumerable<(Entity entity, IComponent component)> Get(Metadata metadata, States include = States.All)
        {
            foreach (var data in _data.Slice())
            {
                if (data.IsValid && TryGetStore(data, metadata, include, out var store, out var index))
                    yield return (data.Segment.Entities.items[data.Index], (IComponent)store.GetValue(index));
            }
        }

        [ThreadSafe]
        bool TryGet(Entity entity, in Metadata metadata, out IComponent component, States include)
        {
            ref readonly var data = ref GetData(entity, out var success);
            if (success && TryGet(data, metadata, out component, include)) return true;
            component = default;
            return false;
        }

        [ThreadSafe]
        bool TryGet(in Data data, in Metadata metadata, out IComponent component, States include)
        {
            if (TryGetStore(data, metadata, include, out var store, out var index))
            {
                component = (IComponent)store.GetValue(index);
                return true;
            }

            component = default;
            return false;
        }

        [ThreadSafe]
        bool TryGet(Entity entity, Metadata[] types, out IComponent component, States include)
        {
            ref readonly var data = ref GetData(entity, out var success);
            if (success && TryGet(data, types, out component, include)) return true;
            component = default;
            return false;
        }

        [ThreadSafe]
        bool TryGet(in Data data, Metadata[] types, out IComponent component, States include)
        {
            for (int i = 0; i < types.Length; i++) if (TryGet(data, types[i], out component, include)) return true;
            component = default;
            return false;
        }
    }
}