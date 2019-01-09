using Entia.Core;
using Entia.Modules;
using System;

namespace Entia.Initializers
{
	public sealed class Entity : Initializer<Entia.Entity>
	{
		public readonly Type[] Tags;
		public readonly int[] Components;
		public readonly World World;

		public Entity(Type[] tags, int[] components, World world)
		{
			Tags = tags;
			Components = components;
			World = world;
		}

		public override Result<Unit> Initialize(Entia.Entity instance, object[] instances)
		{
			var tags = World.Tags();
			var components = World.Components();

			tags.Clear(instance);
			components.Clear(instance);

			foreach (var tag in Tags) tags.Set(instance, tag);
			foreach (var component in Components)
			{
				var result = Result.Cast<IComponent>(instances[component]);
				if (result.TryValue(out var value)) components.Set(instance, value);
				else return result;
			}

			return Result.Success();
		}
	}
}
