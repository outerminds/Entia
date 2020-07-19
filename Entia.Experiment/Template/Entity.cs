
using System;
using System.Collections.Generic;
using Entia.Core;
using Entia.Modules;
using Entia.Initializers;
using Entia.Instantiators;
using Entia.Modules.Template;

namespace Entia.Templaters.E
{
    public sealed class Instantiator : Instantiator<Entia.Entity>
    {
        public readonly Entities Entities;
        public Instantiator(Entities entities) { Entities = entities; }
        public override Result<Entia.Entity> Instantiate(object[] instances) => Entities.Create();
    }

    public sealed class Initializer : Initializer<Entia.Entity>
    {
        public readonly int[] Components;
        public readonly World World;

        public Initializer(int[] components, World world)
        {
            Components = components;
            World = world;
        }

        public override Result<Unit> Initialize(Entia.Entity instance, object[] instances)
        {
            try
            {
                var components = World.Components();
                for (int i = 0; i < Components.Length; i++)
                {
                    var reference = Components[i];
                    components.Set(instance, instances[reference] as IComponent);
                }
                return Result.Success();
            }
            catch (Exception exception) { return Result.Exception(exception); }
        }
    }

    sealed class Templater : ITemplater
    {
        public Result<(IInstantiator instantiator, IInitializer initializer)> Template(in Context context, World world)
        {
            if (context.Index == 0 && Result.Cast<Entia.Entity>(context.Value).TryValue(out var entity))
            {
                var indices = new List<int>();
                var templaters = world.Templaters();
                foreach (var component in world.Components().Get(entity))
                {
                    var result = templaters.Template(new Context(component, component.GetType(), context));
                    if (result.IsFailure()) return result.AsFailure();
                    if (result.TryValue(out var reference)) indices.Add(reference.Index);
                }
                return (new Instantiator(world.Entities()), new Initializer(indices.ToArray(), world));
            }

            return (new Instantiators.Constant(context.Value), new Initializers.Identity());
        }
    }
}