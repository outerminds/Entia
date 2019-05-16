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
        /// Clears all components of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if components were cleared; otherwise, <c>false</c>.</returns>
        public bool Clear<T>(States include = States.All) where T : IComponent => ComponentUtility.Abstract<T>.IsConcrete ?
            Clear(ComponentUtility.Abstract<T>.Data, GetEmitters(ComponentUtility.Abstract<T>.Data), include) :
            Clear(ComponentUtility.Abstract<T>.Mask, include);

        /// <summary>
        /// Clears all components of provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The component type.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if components were cleared; otherwise, <c>false</c>.</returns>
        public bool Clear(Type type, States include = States.All) =>
            ComponentUtility.TryGetMetadata(type, out var metadata) ? Clear(metadata, GetEmitters(metadata), include) :
            ComponentUtility.TryGetConcrete(type, out var mask) && Clear(mask, include);

        /// <summary>
        /// Clears all components associated with the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if components were cleared; otherwise, <c>false</c>.</returns>
        public bool Clear(Entity entity, States include = States.All)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Clear(entity, ref data, include);
        }

        /// <summary>
        /// Clears all components.
        /// </summary>
        /// <returns>Returns <c>true</c> if components were cleared; otherwise, <c>false</c>.</returns>
        public bool Clear(States include = States.All)
        {
            var cleared = false;
            foreach (ref var data in _data.Slice()) cleared |= Clear(data.Segment.Entities.items[data.Index], ref data, include);
            return cleared;
        }

        bool Clear(Entity entity, ref Data data, States include)
        {
            ref var slot = ref GetTransientSlot(entity, ref data, Transient.Resolutions.None);
            return Clear(ref slot, include);
        }

        bool Clear(ref Transient.Slot slot, States include)
        {
            var (enabled, disabled) = GetTargetSegments(slot, include);
            return Clear(ref slot, enabled.Types.data, include) | Clear(ref slot, disabled.Types.data, include);
        }

        bool Clear(ref Transient.Slot slot, Metadata[] types, States include)
        {
            var cleared = false;
            for (var i = 0; i < types.Length; i++)
            {
                ref readonly var metadata = ref types[i];
                cleared |= Remove(ref slot, metadata, GetEmitters(metadata), States.All);
            }
            return cleared;
        }

        bool Clear(in Metadata metadata, in Emitters emitters, States include)
        {
            var cleared = false;
            foreach (ref var data in _data.Slice())
                cleared |= data.IsValid && Remove(data.Segment.Entities.items[data.Index], ref data, metadata, emitters, include);
            return cleared;
        }

        bool Clear(BitMask mask, States include)
        {
            var cleared = false;
            foreach (ref var data in _data.Slice())
                cleared |= data.IsValid && Remove(data.Segment.Entities.items[data.Index], ref data, mask, include);
            return cleared;
        }
    }
}