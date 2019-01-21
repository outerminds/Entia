namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Dependers"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
		public static Dependers Dependers(this World world)
        {
            if (world.TryGet<Dependers>(out var module)) return module;
            world.Set(module = new Dependers(world));
            return module;
        }
    }
}
