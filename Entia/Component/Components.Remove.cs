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
        /// Removes components of type <typeparamref name="T"/> associated with the <paramref name="entity"/>.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if the component was removed; otherwise, <c>false</c>.</returns>
        public bool Remove<T>(Entity entity, States include = States.All) where T : IComponent
        {
            ref var data = ref GetData(entity, out var success);
            if (success) return ComponentUtility.Abstract<T>.IsConcrete ?
                Remove(entity, ref data, ComponentUtility.Abstract<T>.Data, GetEmitters(ComponentUtility.Abstract<T>.Data), include) :
                Remove(entity, ref data, ComponentUtility.Abstract<T>.Mask, include);
            return false;
        }

        /// <summary>
        /// Removes components of provided <paramref name="type"/> associated with the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="type">The component type.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if the component was removed; otherwise, <c>false</c>.</returns>
        public bool Remove(Entity entity, Type type, States include = States.All)
        {
            ref var data = ref GetData(entity, out var success);
            if (success) return
                ComponentUtility.TryGetMetadata(type, out var metadata) ? Remove(entity, ref data, metadata, GetEmitters(metadata), include) :
                ComponentUtility.TryGetConcrete(type, out var mask) && Remove(entity, ref data, mask, include);
            return false;
        }

        bool Remove(Entity entity, ref Data data, in Metadata metadata, in Emitters emitters, States include)
        {
            ref var slot = ref GetTransientSlot(entity, ref data, Transient.Resolutions.None);
            return Remove(ref slot, metadata, emitters, include);
        }

        bool Remove(ref Transient.Slot slot, in Metadata metadata, in Emitters emitters, States include)
        {
            if (include.HasAny(States.Enabled) && slot.Enabled.Has(metadata.Index))
            {
                emitters.OnRemove(slot.Entity);
                slot.Enabled.Remove(metadata.Index);
                slot.Resolution.Set(Transient.Resolutions.Move);
                return true;
            }
            else if (include.HasAny(States.Disabled) && slot.Disabled.Has(metadata.Index))
            {
                emitters.OnRemove(slot.Entity);
                slot.Disabled.Remove(metadata.Index);
                return true;
            }

            return false;
        }

        bool Remove(Entity entity, ref Data data, BitMask mask, States include)
        {
            ref var slot = ref GetTransientSlot(entity, ref data, Transient.Resolutions.None);
            return Remove(entity, ref slot, mask, include);
        }

        bool Remove(Entity entity, ref Transient.Slot slot, BitMask mask, States include)
        {
            var removed = false;
            var segment = GetSegment(mask);
            var types = segment.Types.data;
            for (var i = 0; i < types.Length; i++)
            {
                ref readonly var metadata = ref types[i];
                removed |= Remove(ref slot, metadata, GetEmitters(metadata), include);
            }

            return removed;
        }
    }
}