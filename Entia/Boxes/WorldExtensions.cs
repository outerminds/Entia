namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        public static Boxes Boxes(this World world)
        {
            if (world.TryGet<Boxes>(out var module)) return module;
            world.Set(module = new Boxes());
            return module;
        }
    }
}