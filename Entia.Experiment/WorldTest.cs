using System;
using System.Collections.Generic;
using Entia.Modules;

namespace Entia.Experiment
{
    public static class WorldTest
    {
        public static World RandomWorld(int iterations)
        {
            var world = new World();
            var entities = world.Entities();
            var components = world.Components();
            var random = new Random();
            var list = new List<Entity>();

            Entity RandomEntity() => list.Count == 0 ? Entity.Zero : list[random.Next(list.Count)];
            void SetComponent<T>(in T component) where T : struct, IComponent => components.Set(RandomEntity(), component);
            void RemoveComponent<T>() where T : struct, IComponent => components.Remove<T>(RandomEntity());

            for (var i = 0; i < iterations; i++)
            {
                var value1 = random.NextDouble();
                if (value1 < 0.3) list.Add(entities.Create());
                else if (value1 < 0.4)
                {
                    var entity = RandomEntity();
                    if (entities.Destroy(entity)) list.Remove(entity);
                }
                else if (value1 < 0.8)
                {
                    for (var j = 0; j < 5; j++)
                    {
                        var value2 = random.NextDouble();
                        if (value2 < 0.25) SetComponent(new Position { X = (float)value1, Y = (float)value2 });
                        else if (value2 < 0.5) SetComponent(new Velocity { X = (float)value1, Y = (float)value2 });
                        else if (value2 < 0.75) SetComponent(new Lifetime { Remaining = (float)value1 + (float)value2 });
                        else SetComponent(new Mass { Value = (float)value1 + (float)value2 });
                    }
                }
                else
                {
                    var value2 = random.NextDouble();
                    if (value2 < 0.25) RemoveComponent<Position>();
                    else if (value2 < 0.5) RemoveComponent<Velocity>();
                    else if (value2 < 0.75) RemoveComponent<Lifetime>();
                    else RemoveComponent<Mass>();
                }
                world.Resolve();
            }

            return world;
        }
    }
}