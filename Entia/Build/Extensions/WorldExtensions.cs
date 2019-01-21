namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Builders"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
		public static Builders Builders(this World world)
        {
            if (world.TryGet<Builders>(out var module)) return module;
            world.Set(module = new Builders(world));
            return module;
        }
    }
}