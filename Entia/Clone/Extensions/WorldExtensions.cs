namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Cloners"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
        public static Cloners Cloners(this World world)
        {
            if (world.TryGet<Cloners>(out var module)) return module;
            world.Set(module = new Modules.Cloners(world));
            return module;
        }
    }
}