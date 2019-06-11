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
    public sealed partial class Components
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
            return success && Set(entity, component, ref data, ComponentUtility.Concrete<T>.Data);
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
            return success && ComponentUtility.TryGetMetadata(component.GetType(), true, out var metadata) && Set(entity, component, ref data, metadata);
        }

        bool Set<T>(Entity entity, in T component, ref Data data, in Metadata metadata) where T : struct, IComponent
        {
            if (metadata.Kind == Metadata.Kinds.Data)
            {
                var store = GetStore<T>(entity, ref data, metadata, out var adjusted);
                store[adjusted] = component;
            }
            return Set(entity, ref data, metadata, GetDelegates<T>(metadata));
        }

        bool Set(Entity entity, IComponent component, ref Data data, Metadata metadata)
        {
            if (metadata.Kind == Metadata.Kinds.Data)
            {
                var store = GetStore(entity, ref data, metadata, out var adjusted);
                store.SetValue(component, adjusted);
            }

            return Set(entity, ref data, metadata, GetDelegates(metadata));
        }

        bool Set(Entity entity, in Metadata metadata, in Delegates delegates)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Set(entity, ref data, metadata, delegates);
        }

        bool Set(Entity entity, ref Data data, in Metadata metadata, in Delegates delegates)
        {
            ref var slot = ref GetTransientSlot(entity, ref data, Transient.Resolutions.None);
            return slot.Resolution < Transient.Resolutions.Dispose && Set(ref slot, metadata, delegates);
        }

        bool Set(ref Transient.Slot slot, in Metadata metadata, in Delegates delegates)
        {
            if (slot.Mask.Add(metadata.Index) && slot.Lock.Add(metadata.Index))
            {
                delegates.OnAdd(slot.Entity);
                slot.Resolution.Set(Transient.Resolutions.Move);
                slot.Lock.Remove(metadata.Index);
                return true;
            }
            return false;
        }

        bool SetDisabled(ref Transient.Slot slot, in Delegates delegates)
        {
            if (delegates.IsDisabled.Value.IsValid && slot.Mask.Add(delegates.IsDisabled.Value.Index))
            {
                slot.Resolution.Set(Transient.Resolutions.Move);
                return true;
            }

            return false;
        }
    }
}