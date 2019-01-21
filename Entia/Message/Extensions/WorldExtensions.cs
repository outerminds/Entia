namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        /// <summary>
        /// Gets or create the <see cref="Modules.Messages"/> module.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <returns>The module.</returns>
		public static Messages Messages(this World world)
        {
            if (world.TryGet<Messages>(out var module)) return module;
            world.Set(module = new Messages());
            return module;
        }
    }
}