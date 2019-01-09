namespace Entia.Modules
{
	public static partial class WorldExtensions
	{
		public static Controllers Controllers(this World world)
		{
			if (world.TryGet<Controllers>(out var module)) return module;
			world.Set(module = new Controllers(world));
			return module;
		}
	}
}
