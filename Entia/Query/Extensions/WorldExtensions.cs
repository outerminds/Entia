using System.Reflection;
using Entia.Core;
using Entia.Queriers;
using Entia.Queryables;

namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Queriers"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
        public static Queriers Queriers(this World world)
        {
            if (world.TryGet<Queriers>(out var module)) return module;
            world.Set(module = new Modules.Queriers(world));
            return module;
        }
    }

    namespace Query
    {
        public static class DataExtensions
        {
            public static bool Has(in this Data data, Component.Segment segment) =>
                segment.Index < data.Indices.Length && data.Indices[segment.Index] is int index && data.Segments[index] == segment;

            public static bool TryAdd(ref this Data data, Component.Segment segment)
            {
                if (data.Has(segment)) return false;

                var indices = data.Indices;
                var segments = data.Segments;
                ArrayUtility.Set(ref indices, segments.Length, (int)segment.Index);
                ArrayUtility.Add(ref segments, segment);
                data.With(segments, indices);
                return true;
            }

            public static void With(ref this Data data, Component.Segment[] segments = null, int?[] indices = null) =>
                data = new Data(segments ?? data.Segments, indices ?? data.Indices);
        }

        public static class WorldExtensions
        {
            public static Box<Data> Segments<T>(this World world, MemberInfo member) where T : struct, IQueryable
            {
                var boxes = world.Boxes();
                if (boxes.TryGet<Data>(member, out var box)) return box;
                boxes.Set(member, Data.Empty, out box);

                var querier = world.Queriers().Get<T>(member);
                void TryAdd(Component.Segment segment)
                {
                    if (querier.TryQuery(new Context(segment, world), out _)) box.Value.TryAdd(segment);
                }

                foreach (var segment in world.Components().Segments) TryAdd(segment);
                world.Messages().React((in Entia.Messages.Segment.OnCreate message) => TryAdd(message.Segment));
                return box;
            }
        }
    }
}