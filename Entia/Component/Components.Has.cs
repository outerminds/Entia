using Entia.Core;
using Entia.Core.Documentation;
using Entia.Modules.Component;
using System;

namespace Entia.Modules
{
    public sealed partial class Components
    {
        /// <summary>
        /// Determines whether a component exists.
        /// </summary>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if a component was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool Has(States include = States.All)
        {
            foreach (ref var data in _data.Slice()) if (data.IsValid && Has(data, GetTargetData(data), include)) return true;
            return false;
        }

        /// <summary>
        /// Determines whether the <paramref name="entity"/> has any component.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if a component was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool Has(Entity entity, States include = States.All)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Has(data, GetTargetData(data), include);
        }

        /// <summary>
        /// Determines whether the <paramref name="entity"/> has a component of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if the component was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool Has<T>(Entity entity, States include = States.All) where T : IComponent =>
            ComponentUtility.Abstract<T>.TryConcrete(out var metadata) ? Has(entity, metadata, include) :
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

        /// <summary>
        /// Determines whether at least one entity has a component of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if a component was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool Has<T>(States include = States.All) where T : IComponent =>
            ComponentUtility.Abstract<T>.TryConcrete(out var metadata) ? Has(metadata, include) :
            ComponentUtility.TryGetConcrete<T>(out var mask, out var types) && Has((mask, types), include);

        /// <summary>
        /// Determines whether at least one entity has a component of provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The component type.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if a component was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool Has(Type type, States include = States.All) =>
            ComponentUtility.TryGetMetadata(type, false, out var metadata) ? Has(metadata, include) :
            ComponentUtility.TryGetConcrete(type, out var mask, out var types) && Has((mask, types), include);

        bool Has(in Metadata metadata, States include)
        {
            foreach (ref var data in _data.Slice()) if (data.IsValid && Has(data, metadata, include)) return true;
            return false;
        }

        bool Has(in (BitMask mask, Metadata[] types) components, States include)
        {
            foreach (ref var data in _data.Slice()) if (data.IsValid && Has(data, components, include)) return true;
            return false;
        }

        [ThreadSafe]
        bool Has(Entity entity, in Metadata metadata, States include)
        {
            ref readonly var data = ref GetData(entity, out var success);
            return success && Has(data, metadata, include);
        }

        [ThreadSafe]
        bool Has(in Data data, in Metadata metadata, States include) => data.Transient is int transient ?
            Has(_slots.items[transient], metadata, include) :
            Has(data.Segment.Mask, metadata, include);

        [ThreadSafe]
        bool Has(in Data data, in Metadata metadata, in Delegates delegates, States include) => data.Transient is int transient ?
            Has(_slots.items[transient], metadata, delegates, include) :
            Has(data.Segment.Mask, metadata, delegates, include);

        [ThreadSafe]
        bool Has(Entity entity, in (BitMask mask, Metadata[] types) components, States include)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Has(data, components, include);
        }

        [ThreadSafe]
        bool Has(in Data data, in (BitMask mask, Metadata[] types) components, States include) => data.Transient is int transient ?
            Has(_slots.items[transient], components, include) :
            Has(data.Segment.Mask, components, include);

        [ThreadSafe]
        bool Has(in Slot slot, in (BitMask mask, Metadata[] types) components, States include) =>
            slot.Resolution < Resolutions.Dispose && Has(slot.Mask, components, include);

        [ThreadSafe]
        bool Has(in Slot slot, Metadata metadata, States include) =>
            slot.Resolution < Resolutions.Dispose && Has(slot.Mask, metadata, include);

        [ThreadSafe]
        bool Has(in Slot slot, Metadata metadata, in Delegates delegates, States include) =>
            slot.Resolution < Resolutions.Dispose && Has(slot.Mask, metadata, delegates, include);

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
            delegates.Enabled || include.HasAll(States.All) ? mask.Has(metadata.Index) :
            include.HasAny(States.All) && include.HasAny(State(mask, metadata, delegates));
    }
}