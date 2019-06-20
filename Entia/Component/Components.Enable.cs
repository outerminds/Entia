using Entia.Modules.Component;
using System;

namespace Entia.Modules
{
    public sealed partial class Components
    {
        public bool Enable<T>(Entity entity) where T : IComponent =>
            ComponentUtility.Abstract<T>.TryConcrete(out var metadata) ? Enable(entity, metadata) :
            ComponentUtility.TryGetConcreteTypes<T>(out var types) && Enable(entity, types);

        public bool Enable(Entity entity, Type type) =>
            ComponentUtility.TryGetMetadata(type, false, out var metadata) ? Enable(entity, metadata) :
            ComponentUtility.TryGetConcreteTypes(type, out var types) && Enable(entity, types);

        public bool Enable(Entity entity)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Enable(entity, ref data, GetTargetTypes(data));
        }

        bool Enable(Entity entity, in Metadata metadata)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Enable(entity, ref data, metadata);
        }

        bool Enable(Entity entity, ref Data data, in Metadata metadata)
        {
            ref var slot = ref GetTransientSlot(entity, ref data, Resolutions.None);
            return slot.Resolution < Resolutions.Dispose && Enable(ref slot, metadata);
        }

        bool Enable(ref Slot slot, in Metadata metadata) =>
            TryGetDelegates(metadata, out var delegates) && Enable(ref slot, delegates);

        bool Enable(ref Slot slot, in Delegates delegates)
        {
            if (RemoveDisabled(ref slot, delegates))
            {
                delegates.OnEnable(slot.Entity);
                return true;
            }
            return false;
        }

        bool Enable(Entity entity, Metadata[] types)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Enable(entity, ref data, types);
        }

        bool Enable(Entity entity, ref Data data, Metadata[] types)
        {
            ref var slot = ref GetTransientSlot(entity, ref data, Resolutions.None);
            var enabled = false;
            for (var i = 0; i < types.Length; i++) enabled |= Enable(ref slot, types[i]);
            return enabled;
        }
    }
}