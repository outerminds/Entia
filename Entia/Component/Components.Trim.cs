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
        /// Removes the components on the <paramref name="target"/> that the <paramref name="source"/> does not have.
        /// </summary>
        /// <param name="source">The source entity.</param>
        /// <param name="target">The target entity.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if a component was removed; otherwise, <c>false</c>.</returns>
        public bool Trim(Entity source, Entity target, States include = States.All)
        {
            ref var sourceData = ref GetData(source, out var sourceSuccess);
            ref var targetData = ref GetData(target, out var targetSuccess);
            if (sourceSuccess && targetSuccess)
            {
                ref var slot = ref GetTransientSlot(target, ref targetData, Transient.Resolutions.None);
                var (enabled, disabled) = GetTargetSegments(slot, include);
                return Trim(sourceData, ref slot, enabled.Types.data) | Trim(sourceData, ref slot, disabled.Types.data);
            }
            return false;
        }

        bool Trim(in Data sourceData, ref Transient.Slot targetSlot, Metadata[] types)
        {
            var trimmed = false;
            for (int i = 0; i < types.Length; i++)
            {
                ref readonly var metadata = ref types[i];
                if (Has(sourceData, metadata.Index, States.All)) continue;
                trimmed |= Remove(ref targetSlot, metadata, GetEmitters(metadata), States.All);
            }
            return trimmed;
        }
    }
}