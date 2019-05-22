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
        public States State<T>(Entity entity) where T : struct, IComponent =>
            ComponentUtility.TryGetMetadata<T>(false, out var metadata) ? State(entity, metadata) :
            ComponentUtility.TryGetConcreteTypes<T>(out var types) ? State(entity, types) :
            States.None;

        [ThreadSafe]
        public States State(Entity entity, Type type) =>
            ComponentUtility.TryGetMetadata(type, false, out var metadata) ? State(entity, metadata) :
            ComponentUtility.TryGetConcreteTypes(type, out var types) ? State(entity, types) :
            States.None;

        [ThreadSafe]
        public States State(Entity entity)
        {
            ref readonly var data = ref GetData(entity, out var success);
            return success ? State(data, GetTargetTypes(data)) : States.None;
        }

        [ThreadSafe]
        States State(Entity entity, in Metadata metadata)
        {
            ref readonly var data = ref GetData(entity, out var success);
            return success ? State(data, metadata) : States.None;
        }

        [ThreadSafe]
        States State(Entity entity, Metadata[] types)
        {
            ref readonly var data = ref GetData(entity, out var success);
            return success ? State(data, types) : States.None;
        }

        [ThreadSafe]
        States State(in Data data, Metadata[] types)
        {
            var state = States.None;
            for (int i = 0; i < types.Length; i++) state |= State(data, types[i]);
            return state;
        }

        [ThreadSafe]
        States State(in Data data, in Metadata metadata) => data.Transient is int transient ?
            State(_transient.Slots.items[transient], metadata) :
            State(data.Segment.Mask, metadata);

        [ThreadSafe]
        States State(in Transient.Slot slot, in Metadata metadata) =>
            slot.Resolution == Transient.Resolutions.Dispose ? States.None : State(slot.Mask, metadata);

        [ThreadSafe]
        internal States State(BitMask mask, in Metadata metadata) =>
            TryGetDelegates(metadata, out var delegates) ? State(mask, metadata, delegates) : States.None;

        [ThreadSafe]
        States State(BitMask mask, in Metadata metadata, in Delegates delegates) => mask.Has(metadata.Index) ?
            IsDisabled(mask, delegates) ? States.Disabled : States.Enabled :
            States.None;

        [ThreadSafe]
        bool IsDisabled(BitMask mask, in Delegates delegates) =>
            delegates.IsDisabled.IsValueCreated && IsDisabled(mask, delegates.IsDisabled.Value);

        [ThreadSafe]
        bool IsDisabled(BitMask mask, in Metadata disabled) => disabled.IsValid && mask.Has(disabled.Index);
    }
}