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
        public bool Disable<T>(Entity entity) where T : IComponent =>
            ComponentUtility.TryGetMetadata<T>(false, out var metadata) ? Disable(entity, metadata) :
            ComponentUtility.TryGetConcreteTypes<T>(out var types) && Disable(entity, types);

        public bool Disable(Entity entity, Type type) =>
            ComponentUtility.TryGetMetadata(type, false, out var metadata) ? Disable(entity, metadata) :
            ComponentUtility.TryGetConcreteTypes(type, out var types) && Disable(entity, types);

        public bool Disable(Entity entity)
        {
            ref var data = ref GetData(entity, out var success);
            if (success)
            {
                var segment = GetTargetSegment(data);
                return Disable(entity, ref data, segment.Types);
            }
            return false;
        }

        bool Disable(Entity entity, in Metadata metadata)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Disable(entity, ref data, metadata);
        }

        bool Disable(Entity entity, ref Data data, in Metadata metadata) =>
            TryGetDelegates(metadata, out var delegates) &&
            Set(entity, ref data, metadata, GetDelegates(delegates.IsDisabled.Value));

        bool Disable(Entity entity, Metadata[] types)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Disable(entity, ref data, types);
        }

        bool Disable(Entity entity, ref Data data, Metadata[] types)
        {
            var disabled = false;
            for (var i = 0; i < types.Length; i++) disabled |= Disable(entity, ref data, types[i]);
            return disabled;
        }
    }
}