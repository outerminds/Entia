namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Components"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
        public static Components Components(this World world)
        {
            if (world.TryGet<Components>(out var module)) return module;
            world.Set(module = new Modules.Components(world.Entities(), world.Messages()));
            return module;
        }
    }
}