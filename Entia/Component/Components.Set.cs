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
        /// Sets a default component of type <typeparamref name="T"/> associated with the <paramref name="entity"/>.
        /// If the component is missing, it is added.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>Returns <c>true</c> if the component was added; otherwise, <c>false</c>.</returns>
        public bool Set<T>(Entity entity) where T : struct, IComponent => Set(entity, Default<T>());

        /// <summary>
        /// Sets the <paramref name="component"/> of type <typeparamref name="T"/> associated with the <paramref name="entity"/>.
        /// If the component is missing, it is added.
        /// </summary>
        /// <typeparam name="T">The concrete component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="component">The component.</param>
        /// <returns>Returns <c>true</c> if the component was added; otherwise, <c>false</c>.</returns>
        public bool Set<T>(Entity entity, in T component) where T : struct, IComponent
        {
            ref var data = ref GetData(entity, out var success);
            if (success)
            {
                ref var slot = ref GetTransientSlot(entity, ref data, Transient.Resolutions.None);
                if (slot.Resolution == Transient.Resolutions.Dispose) return false;

                GetStore<T>(ref data, out var store, out var adjusted);
                store[adjusted] = component;
                return Set(ref slot, ComponentUtility.Concrete<T>.Data, GetEmitters<T>());
            }

            return false;
        }

        /// <summary>
        /// Sets a default component of provided type <paramref name="type"/> associated with the <paramref name="entity"/>.
        /// If the component is missing, it is added.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="type">The concrete component type.</param>
        /// <returns>Returns <c>true</c> if the component was added; otherwise, <c>false</c>.</returns>
        public bool Set(Entity entity, Type type) => TryDefault(type, out var component) && Set(entity, component);

        /// <summary>
        /// Sets the <paramref name="component"/> associated with the <paramref name="entity"/>.
        /// If the component is missing, it is added.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="component">The component.</param>
        /// <returns>Returns <c>true</c> if the component was added; otherwise, <c>false</c>.</returns>
        public bool Set(Entity entity, IComponent component)
        {
            if (component == null) return false;

            ref var data = ref GetData(entity, out var success);
            if (success && ComponentUtility.TryGetMetadata(component.GetType(), out var metadata))
            {
                ref var slot = ref GetTransientSlot(entity, ref data, Transient.Resolutions.None);
                if (slot.Resolution == Transient.Resolutions.Dispose) return false;

                GetStore(ref data, metadata, out var store, out var adjusted);
                store.SetValue(component, adjusted);
                return Set(ref slot, metadata, GetEmitters(metadata));
            }

            return false;
        }

        bool Set(ref Transient.Slot slot, in Metadata metadata, in Emitters emitters)
        {
            if (slot.Disabled.Has(metadata.Index)) return false;
            else if (slot.Enabled.Add(metadata.Index))
            {
                slot.Resolution.Set(Transient.Resolutions.Move);
                emitters.OnAdd(slot.Entity);
                return true;
            }
            else return false;
        }
    }
}