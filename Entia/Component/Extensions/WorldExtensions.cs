namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        public static Components Components(this World world)
        {
            if (world.TryGet<Components>(out var module)) return module;
            world.Set(module = new Modules.Components(world.Entities(), world.Messages()));
            return module;
        }
    }
}