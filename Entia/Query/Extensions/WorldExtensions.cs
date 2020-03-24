namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Queriers"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
        public static Queriers Queriers(this World world)
        {
            if (world.TryGet<Queriers>(out var module)) return module;
            world.Set(module = new Queriers(world));
            return module;
        }
    }
}