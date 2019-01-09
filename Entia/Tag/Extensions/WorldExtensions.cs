namespace Entia.Modules
{
	public static partial class WorldExtensions
	{
		public static Tags Tags(this World world)
		{
			if (world.TryGet<Tags>(out var module)) return module;
			world.Set(module = new Tags(world.Entities(), world.Messages()));
			return module;
		}
	}
}