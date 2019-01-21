namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Resources"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
		public static Resources Resources(this World world)
        {
            if (world.TryGet<Resources>(out var module)) return module;
            world.Set(module = new Resources());
            return module;
        }
    }
}