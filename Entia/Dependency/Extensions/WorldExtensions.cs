namespace Entia.Modules
{
	public static partial class WorldExtensions
	{
		public static Dependers Dependers(this World world)
		{
			if (world.TryGet<Dependers>(out var module)) return module;
			world.Set(module = new Dependers(world));
			return module;
		}
	}
}
