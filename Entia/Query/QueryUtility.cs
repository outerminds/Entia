using System;
using System.Reflection;
using Entia.Core;
using Entia.Modules.Component;
using Entia.Queriers;
using Entia.Queryables;

namespace Entia.Modules.Query
{
    public static class QueryUtility
    {
        public readonly struct Data
        {
            public static readonly Data Empty = new Data(Array.Empty<Component.Segment>(), Array.Empty<int?>());

            public readonly Component.Segment[] Segments;
            public readonly int?[] Indices;

            public Data(Component.Segment[] segments, int?[] indices)
            {
                Segments = segments;
                Indices = indices;
            }

            public bool Has(Component.Segment segment) =>
                segment.Index < Indices.Length && Indices[segment.Index] is int index && Segments[index] == segment;

            public Data With(Component.Segment[] segments = null, int?[] indices = null) =>
                new Data(segments ?? Segments, indices ?? Indices);
        }

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

        public static bool TryAsPointer(this Type type, out Type pointer)
        {
            if (type.IsPointer && type.GetElementType() is Type element && ComponentUtility.IsConcrete(element))
            {
                pointer = typeof(Pointer<>).MakeGenericType(element);
                return true;
            }

            pointer = default;
            return false;
        }

        public static Box<Data> Segments<T>(this World world, MemberInfo member) where T : struct, IQueryable
        {
            var boxes = world.Boxes();
            if (boxes.TryGet<Data>(member, out var box)) return box;
            boxes.Set(member, Data.Empty, out box);

            var querier = world.Queriers().Get<T>(member);
            void TryAdd(Segment segment)
            {
                if (querier.TryQuery(new Context(segment, world), out _)) box.Value.TryAdd(segment);
            }

            foreach (var segment in world.Components().Segments) TryAdd(segment);
            world.Messages().React((in Entia.Messages.Segment.OnCreate message) => TryAdd(message.Segment));
            return box;
        }

        static bool TryAdd(ref this Data data, Segment segment)
        {
            if (data.Has(segment)) return false;

            var indices = data.Indices;
            var segments = data.Segments;
            ArrayUtility.Set(ref indices, segments.Length, (int)segment.Index);
            ArrayUtility.Add(ref segments, segment);
            data = data.With(segments, indices);
            return true;
        }
    }
}