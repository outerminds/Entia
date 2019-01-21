namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Schedulers"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
		public static Schedulers Schedulers(this World world)
        {
            if (world.TryGet<Schedulers>(out var module)) return module;
            world.Set(module = new Schedulers(world));
            return module;
        }
    }
}
