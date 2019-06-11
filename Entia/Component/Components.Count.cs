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
        /// Counts the components associated with the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>The number of components.</returns>
        [ThreadSafe]
        public int Count(Entity entity, States include = States.All)
        {
            ref var data = ref GetData(entity, out var success);
            return success ? Count(data, GetTargetTypes(data), include) : 0;
        }

        /// <summary>
        /// Counts all the components of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>The number of components.</returns>
        [ThreadSafe]
        public int Count<T>(States include = States.All) where T : IComponent =>
            ComponentUtility.Abstract<T>.TryConcrete(out var metadata) ? Count(metadata, include) :
            ComponentUtility.TryGetConcreteTypes<T>(out var types) ? Count(types, include) :
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
            ComponentUtility.TryGetConcreteTypes(type, out var types) ? Count(types, include) :
            0;

        /// <summary>
        /// Counts all the components.
        /// </summary>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>The number of components.</returns>
        [ThreadSafe]
        public int Count(States include = States.All)
        {
            var count = 0;
            foreach (ref var data in _data.Slice()) if (data.IsValid) count += Count(data, GetTargetTypes(data), include);
            return count;
        }

        [ThreadSafe]
        int Count(in Metadata metadata, States include)
        {
            var count = 0;
            foreach (ref var data in _data.Slice()) if (data.IsValid && Has(data, metadata, include)) count++;
            return count;
        }

        [ThreadSafe]
        int Count(Metadata[] types, States include)
        {
            var count = 0;
            foreach (ref var data in _data.Slice()) if (data.IsValid) count += Count(data, types, include);
            return count;
        }

        int Count(in Data data, Metadata[] types, States include)
        {
            var count = 0;
            for (int i = 0; i < types.Length; i++) if (Has(data, types[i], include)) count++;
            return count;
        }
    }
}