namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Families"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
        public static Families Families(this World world)
        {
            if (world.TryGet<Families>(out var module)) return module;
            world.Set(module = new Families(world.Messages(), world.Entities()));
            return module;
        }
    }
}
