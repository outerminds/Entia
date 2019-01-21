namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Groups"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
        public static Groups Groups(this World world)
        {
            if (world.TryGet<Groups>(out var module)) return module;
            world.Set(module = new Modules.Groups(world));
            return module;
        }
    }
}