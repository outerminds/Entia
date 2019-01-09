namespace Entia.Modules
{
	public static partial class WorldExtensions
	{
		public static Groups Groups(this World world)
		{
			if (world.TryGet<Groups>(out var module)) return module;
			world.Set(module = new Groups(world.Entities(), world.Queriers(), world.Messages()));
			return module;
		}
	}
}