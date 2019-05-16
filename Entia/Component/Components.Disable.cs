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
        public bool Disable<T>(Entity entity) where T : IComponent => ComponentUtility.Abstract<T>.IsConcrete ?
            Disable(entity, ComponentUtility.Abstract<T>.Data, GetEmitters(ComponentUtility.Abstract<T>.Data)) :
            Disable(entity, ComponentUtility.Abstract<T>.Mask);

        public bool Disable(Entity entity, Type type) =>
            ComponentUtility.TryGetMetadata(type, out var metadata) ? Disable(entity, metadata, GetEmitters(metadata)) :
            ComponentUtility.TryGetConcrete(type, out var mask) && Disable(entity, mask);

        public bool Disable(Entity entity)
        {
            ref var data = ref GetData(entity, out var success);
            if (success)
            {
                ref var slot = ref GetTransientSlot(entity, ref data, Transient.Resolutions.None);
                var segment = GetSegment(slot.Enabled);
                return Disable(ref slot, segment.Types.data);
            }
            return false;
        }

        bool Disable(Entity entity, in Metadata metadata, in Emitters emitters)
        {
            ref var data = ref GetData(entity, out var success);
            if (success)
            {
                ref var slot = ref GetTransientSlot(entity, ref data, Transient.Resolutions.None);
                return Disable(ref slot, metadata, emitters);
            }

            return false;
        }

        bool Disable(Entity entity, BitMask mask)
        {
            ref var data = ref GetData(entity, out var success);
            if (success)
            {
                ref var slot = ref GetTransientSlot(entity, ref data, Transient.Resolutions.None);
                var segment = GetSegment(mask);
                return Disable(ref slot, segment.Types.data);
            }

            return false;
        }

        bool Disable(ref Transient.Slot slot, Metadata[] types)
        {
            var disabled = false;
            for (var i = 0; i < types.Length; i++)
            {
                ref readonly var metadata = ref types[i];
                disabled |= Disable(ref slot, metadata, GetEmitters(metadata));
            }
            return disabled;
        }

        bool Disable(ref Transient.Slot slot, in Metadata metadata, in Emitters emitters)
        {
            if (slot.Enabled.Remove(metadata.Index))
            {
                slot.Disabled.Add(metadata.Index);
                slot.Resolution.Set(Transient.Resolutions.Move);
                emitters.OnDisable(slot.Entity);
                return true;
            }

            return false;
        }
    }
}