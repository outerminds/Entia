namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Templaters"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
		public static Templaters Templaters(this World world)
        {
            if (world.TryGet<Templaters>(out var module)) return module;
            world.Set(module = new Templaters(world));
            return module;
        }
    }
}