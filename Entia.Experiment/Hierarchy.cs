using Entia.Experiment.Components;
using Entia.Modules;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;
using Entia.Stores;
using System.Collections.Generic;

namespace Entia.Experiment.Queryables
{
	public struct Parent<T> : IQueryable<Queriers.Parent<T>> where T : struct, IQueryable
	{
		public readonly T Value;
		public Parent(T value) { Value = value; }
	}

	public struct Ancestor<T> : IQueryable<Queriers.Ancestor<T>> where T : struct, IQueryable
	{
		public readonly T Value;
		public Ancestor(T value) { Value = value; }
	}
}

namespace Entia.Experiment.Queriers
{
	public sealed class Parent<T> : Querier<Queryables.Parent<T>> where T : struct, IQueryable
	{
		public override Query<Queryables.Parent<T>> Query(World world)
		{
			var query = world.Queriers().Query<T>();
			return new Query<Queryables.Parent<T>>(
				world.Queriers().Query<Read<Hierarchy>>(),
				(Entity entity, out Queryables.Parent<T> value) =>
				{
					if (world.Components().TryRead<Hierarchy>(entity, out var hierarchy) &&
						query.TryGet(hierarchy.Value.Parent, out var item))
					{
						value = new Queryables.Parent<T>(item);
						return true;
					}

					value = default;
					return false;
				});
		}
	}

	public sealed class Ancestor<T> : Querier<Queryables.Ancestor<T>> where T : struct, IQueryable
	{
		public override Query<Queryables.Ancestor<T>> Query(World world)
		{
			var query = world.Queriers().Query<T>();
			return new Query<Queryables.Ancestor<T>>(
				world.Queriers().Query<Read<Hierarchy>>(),
				(Entity entity, out Queryables.Ancestor<T> value) =>
				{
					var current = entity;
					while (world.Components().TryRead<Hierarchy>(current, out var hierarchy))
					{
						current = hierarchy.Value.Parent;
						if (query.TryGet(current, out var item))
						{
							value = new Queryables.Ancestor<T>(item);
							return true;
						}
					}

					value = default;
					return false;
				});
		}
	}
}

namespace Entia.Experiment.Components
{
	public struct Hierarchy : IComponent, IStorable<Stores.Factories.Indexed<Hierarchy>>
	{
		public Entity Parent;
		public List<Entity> Children;
	}

	public static class HierarchyExtensions
	{
		//public static Entity Root(this Components<Hierarchy> components, Entity entity) =>
		//	components.Parent(entity) is Entity parent ? components.Root(parent) : entity;

		//public static Entity? Parent(this Components<Hierarchy> components, Entity entity) =>
		//	components.Get(entity)?.Value().Parent;

		//public static IEnumerable<Entity> Ancestors(this Components<Hierarchy> components, Entity entity)
		//{
		//	var current = components.Parent(entity);
		//	while (current.HasValue)
		//	{
		//		yield return current.Value;
		//		current = components.Parent(current.Value);
		//	}
		//}

		//public static IEnumerable<Entity> Children(this Components<Hierarchy> components, Entity entity) =>
		//	components.Get(entity)?.Value().Children ?? Enumerable.Empty<Entity>();

		//public static IEnumerable<Entity> Descendants(this Components<Hierarchy> components, Entity entity) =>
		//	components.Children(entity).SelectMany(child => components.Descendants(child).Append(child));

		//public static IEnumerable<Entity> Family(this Components<Hierarchy> components, Entity entity) =>
		//	components.Descendants(components.Root(entity)).Append(entity);

		//public static IEnumerable<Entity> Siblings(this Components<Hierarchy> components, Entity entity) =>
		//	components.Parent(entity) is Entity parent ?
		//		components.Children(parent).Where(sibling => sibling != entity) :
		//		Enumerable.Empty<Entity>();

		//public static bool Adopt(this Components<Hierarchy> components, Entity child, Entity parent)
		//{
		//	components.Reject(child);
		//	if (components.TryGet(child, out var childWrite))
		//	{
		//		ref var childHierarchy = ref childWrite.Value();
		//		if (childHierarchy.Parent != (childHierarchy.Parent = parent) && components.TryGet(parent, out var parentWrite))
		//		{
		//			ref var parentHierarchy = ref parentWrite.Value();
		//			parentHierarchy.Children.Add(child);
		//			return true;
		//		}
		//	}

		//	return false;
		//}

		//public static bool Reject(this Components<Hierarchy> components, Entity child)
		//{
		//	if (components.TryGet(child, out var childWrite))
		//	{
		//		ref var childHierarchy = ref childWrite.Value();
		//		if (childHierarchy.Parent is Entity parent && components.TryGet(parent, out var parentWrite))
		//		{
		//			ref var parentHierarchy = ref parentWrite.Value();
		//			childHierarchy.Parent = Entity.Zero;
		//			parentHierarchy.Children.Remove(child);
		//			return true;
		//		}
		//	}

		//	return false;
		//}
	}
}
