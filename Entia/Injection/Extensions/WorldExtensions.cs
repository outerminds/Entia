namespace Entia.Modules
{
	public static partial class WorldExtensions
	{
		public static Injectors Injectors(this World world)
		{
			if (world.TryGet<Injectors>(out var module)) return module;
			world.Set(module = new Injectors(world));
			return module;
		}
	}
}