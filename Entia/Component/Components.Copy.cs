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
        /// Copies components of type <typeparamref name="T"/> from the <paramref name="source"/> and sets them on the <paramref name="target"/>.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="source">The source entity.</param>
        /// <param name="target">The target entity.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if the cloning was successful; otherwise, <c>false</c>.</returns>
        public bool Copy<T>(Entity source, Entity target, States include = States.All) where T : IComponent => ComponentUtility.Abstract<T>.IsConcrete ?
            Copy(source, target, ComponentUtility.Abstract<T>.Data, GetEmitters(ComponentUtility.Abstract<T>.Data), include) :
            Copy(source, target, ComponentUtility.Abstract<T>.Mask, include);

        /// <summary>
        /// Copies components of provided <paramref name="type"/> from the <paramref name="source"/> and sets them on the <paramref name="target"/>.
        /// </summary>
        /// <param name="source">The source entity.</param>
        /// <param name="target">The target entity.</param>
        /// <param name="type">The component type.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if the cloning was successful; otherwise, <c>false</c>.</returns>
        public bool Copy(Entity source, Entity target, Type type, States include = States.All) =>
            ComponentUtility.TryGetMetadata(type, out var metadata) ? Copy(source, target, metadata, GetEmitters(metadata), include) :
            ComponentUtility.TryGetConcrete(type, out var mask) && Copy(source, target, mask, include);

        /// <summary>
        /// Copies all the components from the <paramref name="source"/> and sets them on the <paramref name="target"/>.
        /// </summary>
        /// <param name="source">The source entity.</param>
        /// <param name="target">The target entity.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if the cloning was successful; otherwise, <c>false</c>.</returns>
        public bool Copy(Entity source, Entity target, States include = States.All)
        {
            if (source == target) return true;

            ref var sourceData = ref GetData(source, out var sourceSuccess);
            ref var targetData = ref GetData(target, out var targetSuccess);
            if (sourceSuccess && targetSuccess)
            {
                var (enabled, disabled) = GetTargetSegments(sourceData, include);
                ref var slot = ref GetTransientSlot(target, ref targetData, Transient.Resolutions.Move);
                Copy(sourceData, ref targetData, ref slot, enabled.Types.data, include);
                Copy(sourceData, ref targetData, ref slot, disabled.Types.data, include);
                return true;
            }

            return false;
        }

        void Copy(in Data source, ref Data target, ref Transient.Slot slot, Metadata[] types, States include)
        {
            for (var i = 0; i < types.Length; i++)
            {
                ref readonly var metadata = ref types[i];
                Copy(source, ref target, ref slot, metadata, GetEmitters(metadata), include);
            }
        }

        bool Copy(Entity source, Entity target, in Metadata metadata, in Emitters emitters, States include)
        {
            ref var sourceData = ref GetData(source, out var sourceSuccess);
            ref var targetData = ref GetData(target, out var targetSuccess);
            if (sourceSuccess && targetSuccess)
            {
                ref var slot = ref GetTransientSlot(target, ref targetData, Transient.Resolutions.None);
                Copy(sourceData, ref targetData, ref slot, metadata, emitters, include);
                return true;
            }
            return false;
        }

        bool Copy(Entity source, Entity target, BitMask mask, States include)
        {
            ref var sourceData = ref GetData(source, out var sourceSuccess);
            ref var targetData = ref GetData(target, out var targetSuccess);
            if (sourceSuccess && targetSuccess)
            {
                ref var slot = ref GetTransientSlot(target, ref targetData, Transient.Resolutions.None);
                var segment = GetSegment(mask);
                var types = segment.Types.data;
                for (var i = 0; i < types.Length; i++)
                {
                    ref readonly var metadata = ref types[i];
                    Copy(sourceData, ref targetData, ref slot, metadata, GetEmitters(metadata), include);
                }
                return true;
            }
            return false;
        }

        bool Copy(in Data source, ref Data target, ref Transient.Slot slot, in Metadata metadata, in Emitters emitters, States include)
        {
            if (Copy(metadata, source, ref target, include))
            {
                if (slot.Disabled.Has(metadata.Index)) return false;
                else if (slot.Enabled.Add(metadata.Index))
                {
                    slot.Resolution.Set(Transient.Resolutions.Move);
                    emitters.OnAdd(slot.Entity);
                    return true;
                }
            }
            return false;
        }

        bool Copy(in Metadata metadata, in Data source, ref Data target, States include)
        {
            if (TryGetStore(source, metadata, include, out var sourceStore, out var sourceIndex) &&
                GetStore(ref target, metadata, out var targetStore, out var targetIndex))
            {
                Array.Copy(sourceStore, sourceIndex, targetStore, targetIndex, 1);
                return true;
            }
            return false;
        }
    }
}