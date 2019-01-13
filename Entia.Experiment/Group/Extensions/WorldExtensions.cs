namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        public static Groups3 Groups3(this World world)
        {
            if (world.TryGet<Groups3>(out var module)) return module;
            world.Set(module = new Modules.Groups3(world.Components3(), world.Queriers2(), world.Messages()));
            return module;
        }
    }
}