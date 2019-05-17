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
        public bool Has<T>(Entity entity, States include = States.All) where T : IComponent =>
            ComponentUtility.TryGetMetadata<T>(false, out var metadata) ? Has(entity, metadata, include) :
            ComponentUtility.TryGetConcrete<T>(out var mask, out var types) && Has(entity, (mask, types), include);

        /// <summary>
        /// Determines whether the <paramref name="entity"/> has a component of provided <paramref name="type"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="type">The component type.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if the component was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool Has(Entity entity, Type type, States include = States.All) =>
            ComponentUtility.TryGetMetadata(type, false, out var metadata) ? Has(entity, metadata, include) :
            ComponentUtility.TryGetConcrete(type, out var mask, out var types) && Has(entity, (mask, types), include);

        [ThreadSafe]
        bool Has(Entity entity, in Metadata metadata, States include)
        {
            ref readonly var data = ref GetData(entity, out var success);
            return success && Has(data, metadata, include);
        }

        [ThreadSafe]
        bool Has(in Data data, in Metadata metadata, States include) => data.Transient is int transient ?
            Has(_transient.Slots.items[transient], metadata, include) :
            Has(data.Segment.Mask, metadata, include);

        [ThreadSafe]
        bool Has(in Data data, in Metadata metadata, in Delegates delegates, States include) => data.Transient is int transient ?
            Has(_transient.Slots.items[transient], metadata, delegates, include) :
            Has(data.Segment.Mask, metadata, delegates, include);

        [ThreadSafe]
        bool Has(Entity entity, in (BitMask mask, Metadata[] types) components, States include)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Has(data, components, include);
        }

        [ThreadSafe]
        bool Has(in Data data, in (BitMask mask, Metadata[] types) components, States include) => data.Transient is int transient ?
            Has(_transient.Slots.items[transient], components, include) :
            Has(data.Segment.Mask, components, include);

        [ThreadSafe]
        bool Has(in Transient.Slot slot, in (BitMask mask, Metadata[] types) components, States include) =>
            slot.Resolution < Transient.Resolutions.Dispose && Has(slot.Mask, components, include);

        [ThreadSafe]
        bool Has(in Transient.Slot slot, Metadata metadata, States include) =>
            slot.Resolution < Transient.Resolutions.Dispose && Has(slot.Mask, metadata, include);

        [ThreadSafe]
        bool Has(in Transient.Slot slot, Metadata metadata, in Delegates delegates, States include) =>
            slot.Resolution < Transient.Resolutions.Dispose && Has(slot.Mask, metadata, delegates, include);

        [ThreadSafe]
        bool Has(BitMask mask, in (BitMask mask, Metadata[] types) components, States include)
        {
            if (include.HasAll(States.All)) return mask.HasAny(components.mask);
            if (include.HasNone(States.All)) return false;
            for (int i = 0; i < components.types.Length; i++) if (include.HasAny(State(mask, components.types[i]))) return true;
            return false;
        }

        [ThreadSafe]
        internal bool Has(BitMask mask, in Metadata metadata, States include) =>
            TryGetDelegates(metadata, out var delegates) && Has(mask, metadata, delegates, include);

        [ThreadSafe]
        bool Has(BitMask mask, in Metadata metadata, in Delegates delegates, States include) =>
            include.HasAll(States.All) ? mask.Has(metadata.Index) :
            include.HasAny(States.All) && include.HasAny(State(mask, metadata, delegates));
    }
}