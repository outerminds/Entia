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
        [ThreadSafe]
        public States State<T>(Entity entity) where T : struct, IComponent
        {
            ref readonly var data = ref GetData(entity, out var success);
            return success && ComponentUtility.TryGetMetadata<T>(false, out var metadata) ? State(data, metadata) : States.None;
        }

        [ThreadSafe]
        public States State(Entity entity, Type type)
        {
            ref readonly var data = ref GetData(entity, out var success);
            return success && ComponentUtility.TryGetMetadata(type, false, out var metadata) ? State(data, metadata) : States.None;
        }

        [ThreadSafe]
        public States State(Entity entity)
        {
            ref readonly var data = ref GetData(entity, out var success);
            return success ? State(data) : States.None;
        }

        [ThreadSafe]
        States State(in Data data)
        {
            ref readonly var metadata = ref ComponentUtility.Cache<IsDisabled>.Data;
            if (data.Transient is int transient)
            {
                ref readonly var slot = ref _transient.Slots.items[transient];
                if (slot.Resolution == Transient.Resolutions.Dispose) return States.None;
                return State(slot.Mask);
            }
            else return State(data.Segment.Mask);
        }

        [ThreadSafe]
        States State(in Data data, in Metadata metadata) => data.Transient is int transient ?
            State(_transient.Slots.items[transient], metadata) :
            State(data.Segment.Mask, metadata);

        [ThreadSafe]
        States State(in Transient.Slot slot, in Metadata metadata) =>
            slot.Resolution == Transient.Resolutions.Dispose ? States.None : State(slot.Mask, metadata);

        [ThreadSafe]
        States State(BitMask mask, in Metadata metadata) => TryGetDelegates(metadata, out var delegates) ?
            State(mask, metadata, delegates) : States.None;

        [ThreadSafe]
        States State(BitMask mask, in Metadata metadata, in Delegates delegates)
        {
            if (mask.Has(metadata.Index))
                return delegates.IsDisabled.IsValueCreated && IsDisabled(mask, delegates.IsDisabled.Value) ? States.Disabled : States.Enabled;
            else
                return States.None;
        }

        [ThreadSafe]
        internal States State(BitMask mask) => IsDisabled(mask) ? States.Disabled : States.Enabled;

        [ThreadSafe]
        bool IsDisabled(BitMask mask) => mask.Has(ComponentUtility.Cache<IsDisabled>.Data.Index);

        [ThreadSafe]
        bool IsDisabled(BitMask mask, in Metadata disabled) => disabled.IsValid && mask.Has(disabled.Index);
    }
}