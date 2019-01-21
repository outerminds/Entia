namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Entities"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
        public static Entities Entities(this World world)
        {
            if (world.TryGet<Entities>(out var module)) return module;
            world.Set(module = new Entities(world.Messages()));
            return module;
        }
    }
}