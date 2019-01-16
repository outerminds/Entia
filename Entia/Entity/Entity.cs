using System;
using System.Runtime.InteropServices;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;

namespace Entia
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct Entity : IQueryable, IEquatable<Entity>, IComparable<Entity>
    {
        sealed class Querier : Querier<Entity>
        {
            public override bool TryQuery(Segment segment, World world, out Query<Entity> query)
            {
                query = new Query<Entity>(index => segment.Entities.items[index]);
                return true;
            }
        }

        public static readonly Entity Zero;

        [Querier]
        static readonly Querier _querier = new Querier();

        [FieldOffset(0)]
        public readonly ulong Identifier;
        [FieldOffset(0)]
        public readonly int Index;
        [FieldOffset(sizeof(int))]
        public readonly uint Generation;

        internal Entity(int index, uint generation)
        {
            Identifier = default;
            Index = index;
            Generation = generation;
        }

        public int CompareTo(Entity other) => Identifier.CompareTo(other.Identifier);
        public bool Equals(Entity other) => Identifier == other.Identifier;
        public override bool Equals(object obj) => obj is Entity entity && Equals(entity);
        public override int GetHashCode() => Identifier.GetHashCode();
        public override string ToString() => $"{{ Index: {Index}, Generation: {Generation} }}";

        public static bool operator ==(Entity a, Entity b) => a.Equals(b);
        public static bool operator !=(Entity a, Entity b) => !a.Equals(b);
    }
}
