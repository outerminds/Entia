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
        public bool Enable<T>(Entity entity) where T : IComponent =>
            ComponentUtility.TryGetMetadata<T>(false, out var metadata) ? Enable(entity, metadata) :
            ComponentUtility.TryGetConcreteTypes<T>(out var types) && Enable(entity, types);

        public bool Enable(Entity entity, Type type) =>
            ComponentUtility.TryGetMetadata(type, false, out var metadata) ? Enable(entity, metadata) :
            ComponentUtility.TryGetConcreteTypes(type, out var types) && Enable(entity, types);

        public bool Enable(Entity entity) => Remove(entity, typeof(IsDisabled<>));

        bool Enable(Entity entity, in Metadata metadata) =>
            TryGetDelegates(metadata, out var delegates) &&
            Remove(entity, delegates.IsDisabled.Value, States.All);

        bool Enable(Entity entity, Metadata[] types)
        {
            var enabled = false;
            for (var i = 0; i < types.Length; i++) enabled |= Enable(entity, types[i]);
            return enabled;
        }
    }
}