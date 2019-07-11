namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Serializers"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
        public static Serializers Serializers(this World world)
        {
            if (world.TryGet<Serializers>(out var module)) return module;
            world.Set(module = new Serializers(world));
            return module;
        }
    }
}