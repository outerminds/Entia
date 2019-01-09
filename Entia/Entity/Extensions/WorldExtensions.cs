namespace Entia.Modules
{
	public static partial class WorldExtensions
	{
		public static Entities Entities(this World world)
		{
			if (world.TryGet<Entities>(out var module)) return module;
			world.Set(module = new Entities(world.Messages()));
			return module;
		}
	}
}