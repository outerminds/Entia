namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        public static Queriers2 Queriers2(this World world)
        {
            if (world.TryGet<Queriers2>(out var module)) return module;
            world.Set(module = new Modules.Queriers2(world));
            return module;
        }
    }
}