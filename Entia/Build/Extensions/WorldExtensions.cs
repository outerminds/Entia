namespace Entia.Modules
{
	public static partial class WorldExtensions
	{
		public static Builders Builders(this World world)
		{
			if (world.TryGet<Builders>(out var module)) return module;
			world.Set(module = new Builders(world));
			return module;
		}
	}
}