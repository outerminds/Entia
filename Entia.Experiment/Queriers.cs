using Entia.Core;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Entia.Experiment
{
    public unsafe static class QuerierTest
    {
        [None(typeof(Mass))]
        public struct Query : Queryables.IQueryable
        {
            public Entity Entity;
            public Maybe<Read<Mass>> Mass1;
            public Write<Position> Position;
            public Maybe<Read<Mass>> Mass2;
            public Read<Velocity> Velocity;
            public Maybe<Read<Mass>> Mass3;
        }

        public struct Query2 : Queryables.IQueryable
        {
            public Entity Entity1;
            public Query Query;
            public Entity Entity2;
        }

        public unsafe static void Run()
        {
            var random = new Random();
            var world = new World();
            var entities = world.Entities();
            var components = world.Components();
            var queriers = world.Queriers();
            var groups = world.Groups();

            for (int i = 0; i < 100; i++)
            {
                var entity = entities.Create();
                components.Set(entity, new Position { X = i + 1, Y = i + 2, Z = i + 3 });
                components.Set(entity, new Velocity { X = i + 4, Y = i + 5, Z = i + 6 });
                if (random.NextDouble() < 0.5) components.Set(entity, new Mass { Value = i });
            }
            world.Resolve();

            var group = groups.Get(queriers.Get<Query>());
            var group2 = groups.Get(queriers.Get<Query2>());
            foreach (ref readonly var item in group)
            {
                ref var position = ref item.Position.Value;
                ref readonly var velocity = ref item.Velocity.Value;
                position.X += velocity.X;
                position.Y += velocity.Y;
                position.Z += velocity.Z;
            }
        }
    }
}