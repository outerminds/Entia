namespace Entia.Modules
{
	public static partial class WorldExtensions
	{
		public static Analyzers Analyzers(this World world)
		{
			if (world.TryGet<Analyzers>(out var module)) return module;
			world.Set(module = new Analyzers(world));
			return module;
		}
	}
}