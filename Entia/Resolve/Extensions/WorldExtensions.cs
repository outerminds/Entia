namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Resolvers"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
        public static Resolvers Resolvers(this World world)
        {
            if (world.TryGet<Resolvers>(out var module)) return module;
            world.Set(module = new Resolvers(world));
            return module;
        }
    }
}