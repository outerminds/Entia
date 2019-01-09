using Entia.Queryables;
using System;
using System.Runtime.InteropServices;

namespace Entia
{
	[StructLayout(LayoutKind.Explicit)]
	public readonly struct Entity : IQueryable<Queriers.Entity>, IEquatable<Entity>, IComparable<Entity>
	{
		public static readonly Entity Zero;

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
