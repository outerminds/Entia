using Entia.Core;
using Entia.Modules;
using System;

namespace Entia.Initializers
{
    public sealed class Entity : Initializer<Entia.Entity>
    {
        public readonly int[] Components;
        public readonly World World;

        public Entity(int[] components, World world)
        {
            Components = components;
            World = world;
        }

        public override Result<Unit> Initialize(Entia.Entity instance, object[] instances)
        {
            var components = World.Components();
            components.Clear(instance);

            foreach (var component in Components)
            {
                var result = Result.Cast<IComponent>(instances[component]);
                if (result.TryValue(out var value)) components.Set(instance, value);
                else return result;
            }

            return Result.Success();
        }
    }
}
