using System;

namespace Entia.Modules.Query
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
    }
}