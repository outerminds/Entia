namespace Entia.Modules
{
	public static partial class WorldExtensions
	{
		public static Schedulers Schedulers(this World world)
		{
			if (world.TryGet<Schedulers>(out var module)) return module;
			world.Set(module = new Schedulers(world));
			return module;
		}
	}
}
