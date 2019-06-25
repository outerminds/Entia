namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        public static Families Families(this World world)
        {
            if (world.TryGet<Families>(out var module)) return module;
            world.Set(module = new Families(world.Messages(), world.Entities()));
            return module;
        }
    }
}
