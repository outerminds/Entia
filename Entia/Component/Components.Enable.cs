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
        public bool Enable<T>(Entity entity) where T : IComponent => ComponentUtility.Abstract<T>.IsConcrete ?
            Enable(entity, ComponentUtility.Abstract<T>.Data, GetEmitters(ComponentUtility.Abstract<T>.Data)) :
            Enable(entity, ComponentUtility.Abstract<T>.Mask);

        public bool Enable(Entity entity, Type type) =>
            ComponentUtility.TryGetMetadata(type, out var metadata) ? Enable(entity, metadata, GetEmitters(metadata)) :
            ComponentUtility.TryGetConcrete(type, out var mask) && Enable(entity, mask);

        public bool Enable(Entity entity)
        {
            ref readonly var data = ref GetData(entity, out var success);
            if (success && data.Transient is int transient)
            {
                ref readonly var slot = ref _transient.Slots.items[transient];
                var segment = GetSegment(slot.Disabled);
                return Enable(slot, segment.Types.data);
            }
            return false;
        }

        bool Enable(Entity entity, in Metadata metadata, in Emitters emitters)
        {
            ref readonly var data = ref GetData(entity, out var success);
            // NOTE: if the entity has no 'transient', then it can be assumed that no components have been disabled
            if (success && data.Transient is int transient)
            {
                ref readonly var slot = ref _transient.Slots.items[transient];
                return Enable(slot, metadata, emitters);
            }

            return false;
        }

        bool Enable(Entity entity, BitMask mask)
        {
            ref readonly var data = ref GetData(entity, out var success);
            // NOTE: if the entity has no 'transient', then it can be assumed that no components have been disabled
            if (success && data.Transient is int transient)
            {
                ref readonly var slot = ref _transient.Slots.items[transient];
                var segment = GetSegment(mask);
                return Enable(slot, segment.Types.data);
            }

            return false;
        }

        bool Enable(in Transient.Slot slot, Metadata[] types)
        {
            var enabled = false;
            for (var i = 0; i < types.Length; i++)
            {
                ref readonly var metadata = ref types[i];
                enabled |= Enable(slot, metadata, GetEmitters(metadata));
            }
            return enabled;
        }

        bool Enable(in Transient.Slot slot, in Metadata metadata, in Emitters emitters)
        {
            if (slot.Disabled.Remove(metadata.Index))
            {
                slot.Enabled.Add(metadata.Index);
                emitters.OnEnable(slot.Entity);
                return true;
            }

            return false;
        }
    }
}