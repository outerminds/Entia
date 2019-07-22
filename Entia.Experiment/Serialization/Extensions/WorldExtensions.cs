namespace Entia.Experiment
{
    public static class WorldExtensions
    {
        public static Descriptors Descriptors(this World world)
        {
            if (world.TryGet<Descriptors>(out var module)) return module;
            world.Set(module = new Descriptors(world));
            return module;
        }
    }
}