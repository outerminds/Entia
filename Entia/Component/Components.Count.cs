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
        /// Counts the components associated with the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>The number of components.</returns>
        public int Count(Entity entity, States include = States.All)
        {
            ref var data = ref GetData(entity, out var success);
            if (success)
            {
                var segment = GetTargetSegment(data);
                var types = segment.Types;
                if (include.HasAll(States.All)) return types.Length;

                var count = 0;
                for (int i = 0; i < types.Length; i++) if (Has(data, types[i], include)) count++;
                return count;
            }
            return 0;
        }

        /// <summary>
        /// Counts all the components of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>The number of components.</returns>
        [ThreadSafe]
        public int Count<T>(States include = States.All) where T : IComponent =>
            ComponentUtility.TryGetMetadata<T>(false, out var metadata) ? Count(metadata, include) :
            ComponentUtility.TryGetConcrete<T>(out var mask, out var types) ? Count((mask, types), include) :
            0;

        /// <summary>
        /// Counts all the components of provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The component type.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>The number of components.</returns>
        [ThreadSafe]
        public int Count(Type type, States include = States.All) =>
            ComponentUtility.TryGetMetadata(type, false, out var metadata) ? Count(metadata, include) :
            ComponentUtility.TryGetConcrete(type, out var mask, out var types) ? Count((mask, types), include) :
            0;

        [ThreadSafe]
        int Count(in Metadata metadata, States include)
        {
            var count = 0;
            foreach (ref var data in _data.Slice()) if (data.IsValid && Has(data, metadata, include)) count++;
            return count;
        }

        [ThreadSafe]
        int Count(in (BitMask mask, Metadata[] types) components, States include)
        {
            var count = 0;
            foreach (ref var data in _data.Slice()) if (data.IsValid && Has(data, components, include)) count++;
            return count;
        }
    }
}