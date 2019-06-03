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
        /// Copies components of type <typeparamref name="T"/> from the <paramref name="source"/> and sets them on the <paramref name="target"/>.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="source">The source entity.</param>
        /// <param name="target">The target entity.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if the cloning was successful; otherwise, <c>false</c>.</returns>
        public bool Copy<T>(Entity source, Entity target, States include = States.All) where T : IComponent =>
            ComponentUtility.TryGetMetadata<T>(false, out var metadata) ? Copy(source, target, metadata, include) :
            ComponentUtility.TryGetConcreteTypes<T>(out var types) && Copy(source, target, types, include);

        /// <summary>
        /// Copies components of provided <paramref name="type"/> from the <paramref name="source"/> and sets them on the <paramref name="target"/>.
        /// </summary>
        /// <param name="source">The source entity.</param>
        /// <param name="target">The target entity.</param>
        /// <param name="type">The component type.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if the cloning was successful; otherwise, <c>false</c>.</returns>
        public bool Copy(Entity source, Entity target, Type type, States include = States.All) =>
            ComponentUtility.TryGetMetadata(type, false, out var metadata) ? Copy(source, target, metadata, include) :
            ComponentUtility.TryGetConcreteTypes(type, out var types) && Copy(source, target, types, include);

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
                var types = GetTargetTypes(sourceData);
                ref var slot = ref GetTransientSlot(target, ref targetData, Transient.Resolutions.Move);
                for (var i = 0; i < types.Length; i++)
                {
                    ref readonly var metadata = ref types[i];
                    Copy(sourceData, ref targetData, ref slot, metadata, include);
                }
                return true;
            }

            return false;
        }

        bool Copy(Entity source, Entity target, in Metadata metadata, States include)
        {
            ref var sourceData = ref GetData(source, out var sourceSuccess);
            ref var targetData = ref GetData(target, out var targetSuccess);
            if (sourceSuccess && targetSuccess && TryGetDelegates(metadata, out var delegates))
            {
                ref var slot = ref GetTransientSlot(target, ref targetData, Transient.Resolutions.None);
                Copy(sourceData, ref targetData, ref slot, metadata, delegates, include);
                return true;
            }
            return false;
        }

        bool Copy(Entity source, Entity target, Metadata[] types, States include)
        {
            ref var sourceData = ref GetData(source, out var sourceSuccess);
            ref var targetData = ref GetData(target, out var targetSuccess);
            if (sourceSuccess && targetSuccess)
            {
                ref var slot = ref GetTransientSlot(target, ref targetData, Transient.Resolutions.None);
                for (var i = 0; i < types.Length; i++)
                {
                    ref readonly var metadata = ref types[i];
                    Copy(sourceData, ref targetData, ref slot, metadata, include);
                }
                return true;
            }
            return false;
        }

        bool Copy(in Data source, ref Data target, ref Transient.Slot slot, in Metadata metadata, States include) =>
            TryGetDelegates(metadata, out var delegates) && Copy(source, ref target, ref slot, metadata, delegates, include);

        bool Copy(in Data source, ref Data target, ref Transient.Slot slot, in Metadata metadata, in Delegates delegates, States include)
        {
            if (metadata.Kind == Metadata.Kinds.Tag)
                // NOTE: must check 'Has' in case the component has been removed on the source
                return Has(source, metadata, delegates, include) && Set(ref slot, metadata, delegates);
            else if (TryGetStore(source, metadata, include, out var sourceStore, out var sourceIndex))
            {
                var targetStore = GetStore(slot.Entity, ref target, metadata, out var targetIndex);
                Array.Copy(sourceStore, sourceIndex, targetStore, targetIndex, 1);
                return Set(ref slot, metadata, delegates);
            }
            else return false;
        }
    }
}