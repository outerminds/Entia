namespace Entia.Modules
{
	public static partial class WorldExtensions
	{
		public static Resources Resources(this World world)
		{
			if (world.TryGet<Resources>(out var module)) return module;
			world.Set(module = new Resources());
			return module;
		}
	}
}