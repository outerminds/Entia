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
        /// Determines whether the <paramref name="entity"/> has a component of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if the component was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool Has<T>(Entity entity, States include = States.All) where T : IComponent => ComponentUtility.Abstract<T>.IsConcrete ?
            Has(entity, ComponentUtility.Abstract<T>.Data.Index, include) :
            Has(entity, ComponentUtility.Abstract<T>.Mask, include);

        /// <summary>
        /// Determines whether the <paramref name="entity"/> has a component of provided <paramref name="type"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="type">The component type.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if the component was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool Has(Entity entity, Type type, States include = States.All) =>
            ComponentUtility.TryGetMetadata(type, out var metadata) ? Has(entity, metadata.Index, include) :
            ComponentUtility.TryGetConcrete(type, out var mask) && Has(entity, mask, include);

        [ThreadSafe]
        bool Has(Entity entity, BitMask mask, States include)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Has(data, mask, include);
        }

        [ThreadSafe]
        bool Has(Entity entity, int index, States include)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Has(data, index, include);
        }

        [ThreadSafe]
        bool Has(in Data data, BitMask mask, States include)
        {
            if (data.Transient is int transient)
            {
                ref readonly var slot = ref _transient.Slots.items[transient];
                return Has(slot, mask, include);
            }
            return include.HasAny(States.Enabled) && data.Segment.Mask.HasAny(mask);
        }

        [ThreadSafe]
        bool Has(in Data data, int index, States include)
        {
            if (data.Transient is int transient)
            {
                ref readonly var slot = ref _transient.Slots.items[transient];
                return Has(slot, index, include);
            }
            else
                return include.HasAny(States.Enabled) && data.Segment.Mask.Has(index);
        }

        [ThreadSafe]
        bool Has(in Transient.Slot slot, BitMask mask, States include)
        {
            if (slot.Resolution == Transient.Resolutions.Dispose) return false;
            if (include.HasAny(States.Enabled) && slot.Enabled.HasAny(mask)) return true;
            if (include.HasAny(States.Disabled) && slot.Disabled.HasAny(mask)) return true;
            return false;
        }

        [ThreadSafe]
        bool Has(in Transient.Slot slot, int index, States include)
        {
            if (slot.Resolution == Transient.Resolutions.Dispose) return false;
            if (include.HasAny(States.Enabled) && slot.Enabled.Has(index)) return true;
            if (include.HasAny(States.Disabled) && slot.Disabled.Has(index)) return true;
            return false;
        }
    }
}