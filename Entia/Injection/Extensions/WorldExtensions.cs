namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Injectors"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
		public static Injectors Injectors(this World world)
        {
            if (world.TryGet<Injectors>(out var module)) return module;
            world.Set(module = new Injectors(world));
            return module;
        }
    }
}