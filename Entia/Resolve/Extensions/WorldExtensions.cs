namespace Entia.Modules
{
    public static partial class WorldExtensions
    {
        public static Resolvers Resolvers(this World world)
        {
            if (world.TryGet<Resolvers>(out var module)) return module;
            world.Set(module = new Resolvers(world));
            return module;
        }
    }
}