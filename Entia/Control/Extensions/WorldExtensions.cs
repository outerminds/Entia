namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Controllers"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
		public static Controllers Controllers(this World world)
        {
            if (world.TryGet<Controllers>(out var module)) return module;
            world.Set(module = new Controllers(world));
            return module;
        }
    }
}
