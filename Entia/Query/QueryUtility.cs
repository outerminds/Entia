using System;
using Entia.Core;
using Entia.Modules.Component;
using Entia.Queriers;

namespace Entia.Modules.Query
{
    public static class QueryUtility
    {
        public static bool TryMatch(BitMask mask, Type type, World world, States include, out Metadata metadata)
        {
            var components = world.Components();
            if (ComponentUtility.TryGetConcreteTypes(type, out var types))
            {
                for (int i = 0; i < types.Length; i++)
                {
                    metadata = types[i];
                    if (components.Has(mask, metadata, include)) return true;
                }
            }

            metadata = default;
            return false;
        }

        public static Context With(in this Context context, States? include = null) =>
            new Context(context.Segment, context.World, include ?? context.Include);
    }
}