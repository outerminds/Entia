namespace Entia.Modules
{
	public static partial class WorldExtensions
	{
		public static Messages Messages(this World world)
		{
			if (world.TryGet<Messages>(out var module)) return module;
			world.Set(module = new Messages());
			return module;
		}
	}
}