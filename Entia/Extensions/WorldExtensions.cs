namespace Entia.Modules
{
	public static partial class WorldExtensions
	{
		public static void CopyTo(this World source, World world, bool entities = true, bool components = true, bool tags = true, bool resources = false)
		{
			if (entities)
			{
				foreach (var entity in source.Entities())
					source.CopyTo(entity, world, components, tags);
			}

			if (resources) source.Resources().CopyTo(world.Resources());
		}

		public static Entity CopyTo(this World source, Entity entity, World world, bool components = true, bool tags = true)
		{
			var target = world.Entities().Create();
			if (components) source.Components().CopyTo(entity, target, world.Components());
			if (tags) source.Tags().CopyTo(entity, target, world.Tags());
			return target;
		}
	}
}