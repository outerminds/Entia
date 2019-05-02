using System;

namespace Entia.Modules
{
    public static class EntityExtensions
    {
        public static string Name(this Entity entity, World world) =>
            world.TryGet<Modules.Components>(out var components) && components.TryGet<Entia.Components.Debug>(entity, out var debug) ?
            debug.Name : default;

        public static string ToString(this Entity entity, World world) => entity.Name(world) is string name ? $"{name}: {entity}" : $"{entity}";
    }
}