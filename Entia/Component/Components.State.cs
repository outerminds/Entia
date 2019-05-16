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
            if (success) return State(data, ComponentUtility.Concrete<T>.Data);
            else return States.None;
        }

        [ThreadSafe]
        public States State(Entity entity, Type type)
        {
            ref readonly var data = ref GetData(entity, out var success);
            if (success && ComponentUtility.TryGetMetadata(type, out var metadata)) return State(data, metadata);
            else return States.None;
        }

        [ThreadSafe]
        States State(in Data data, in Metadata metadata)
        {
            if (data.Transient is int transient)
            {
                ref readonly var slot = ref _transient.Slots.items[transient];
                if (slot.Enabled.Has(metadata.Index)) return States.Enabled;
                else if (slot.Disabled.Has(metadata.Index)) return States.Disabled;
                else return States.None;
            }
            else if (data.Segment.Mask.Has(metadata.Index)) return States.Enabled;
            else return States.None;
        }
    }
}