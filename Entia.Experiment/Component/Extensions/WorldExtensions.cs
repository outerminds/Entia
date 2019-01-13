namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        public static Components3 Components3(this World world)
        {
            if (world.TryGet<Components3>(out var module)) return module;
            world.Set(module = new Modules.Components3(world.Messages()));
            return module;
        }
    }
}