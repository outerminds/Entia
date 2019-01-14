namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        public static Queriers Queriers(this World world)
        {
            if (world.TryGet<Queriers>(out var module)) return module;
            world.Set(module = new Modules.Queriers(world));
            return module;
        }
    }
}