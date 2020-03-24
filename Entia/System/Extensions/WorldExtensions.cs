namespace Entia.Experimental.Modules
{
    public static partial class WorldExtensions
    {
        public static Systems Systems(this World world)
        {
            if (world.TryGet<Systems>(out var module)) return module;
            world.Set(module = new Systems());
            return module;
        }
    }
}