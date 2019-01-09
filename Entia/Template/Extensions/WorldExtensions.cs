namespace Entia.Modules
{
	public static partial class WorldExtensions
	{
		public static Templaters Templaters(this World world)
		{
			if (world.TryGet<Templaters>(out var module)) return module;
			world.Set(module = new Templaters(world));
			return module;
		}
	}
}