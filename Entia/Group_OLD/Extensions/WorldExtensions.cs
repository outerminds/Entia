namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        public static Groups_OLD Groups_OLD(this World world)
        {
            if (world.TryGet<Groups_OLD>(out var module)) return module;
            world.Set(module = new Groups_OLD(world.Entities(), world.Queriers_OLD(), world.Messages()));
            return module;
        }
    }
}