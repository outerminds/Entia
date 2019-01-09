namespace Entia.Modules
{
	public static partial class WorldExtensions
	{
		public static Stores Stores(this World world)
		{
			if (world.TryGet<Stores>(out var module)) return module;
			world.Set(module = new Stores(world));
			return module;
		}
	}
}