namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Analyzers"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
		public static Analyzers Analyzers(this World world)
        {
            if (world.TryGet<Analyzers>(out var module)) return module;
            world.Set(module = new Analyzers(world));
            return module;
        }
    }
}