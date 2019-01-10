using Entia.Modules;
using Entia.Modules.Query;
using Entia.Queryables;
using Entia.Segments;
using Entia.Stores;
using FsCheck;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Entia.Test
{
    public static class Test
    {
        public struct TagA : ITag { }
        public struct TagB : ITag { }
        public struct ComponentA : IComponent { }
        public struct ComponentB : IComponent { public float Value; }
        public struct IndexedComponentA : IComponent, IStorable<Stores.Factories.Indexed<IndexedComponentA>> { }
        public struct IndexedComponentB : IComponent, IStorable<Stores.Factories.Indexed<IndexedComponentB>> { public float Value; }
        public struct MessageA : IMessage { }
        public struct MessageB : IMessage { }
        public struct SegmentA : ISegment { }
        public struct SegmentB : ISegment { }

        public static void Run(int count = 1600, int size = 1600)
        {
            Console.Clear();

            var generator = Generator.Frequency(
                // World
                (5, Gen.Fresh(() => new ResolveWorld().ToAction())),

                // Entity
                (25, Gen.Fresh(() => new CreateEntity().ToAction())),
                (25, Gen.Fresh(() => new CreateEntity<Default>().ToAction())),
                (25, Gen.Fresh(() => new CreateEntity<SegmentA>().ToAction())),
                (25, Gen.Fresh(() => new CreateEntity<SegmentB>().ToAction())),
                (5, Gen.Fresh(() => new DestroyEntity().ToAction())),
                (5, Gen.Fresh(() => new DestroyEntity<Default>().ToAction())),
                (5, Gen.Fresh(() => new DestroyEntity<SegmentA>().ToAction())),
                (5, Gen.Fresh(() => new DestroyEntity<SegmentB>().ToAction())),
                (1, Gen.Fresh(() => new ClearEntities().ToAction())),

                // Component
                (10, Gen.Fresh(() => new AddComponent<ComponentA>().ToAction())),
                (10, Gen.Fresh(() => new AddComponent<ComponentB>().ToAction())),
                (10, Gen.Fresh(() => new RemoveComponent<ComponentA>().ToAction())),
                (10, Gen.Fresh(() => new RemoveComponent<ComponentB>().ToAction())),
                (3, Gen.Fresh(() => new ClearComponent<ComponentA>().ToAction())),
                (3, Gen.Fresh(() => new ClearComponent<ComponentB>().ToAction())),

                (10, Gen.Fresh(() => new AddComponent<IndexedComponentA>().ToAction())),
                (10, Gen.Fresh(() => new AddComponent<IndexedComponentB>().ToAction())),
                (10, Gen.Fresh(() => new RemoveComponent<IndexedComponentA>().ToAction())),
                (10, Gen.Fresh(() => new RemoveComponent<IndexedComponentB>().ToAction())),
                (3, Gen.Fresh(() => new ClearComponent<IndexedComponentA>().ToAction())),
                (3, Gen.Fresh(() => new ClearComponent<IndexedComponentB>().ToAction())),

                // Tag
                (10, Gen.Fresh(() => new AddTag<TagA>().ToAction())),
                (10, Gen.Fresh(() => new AddTag<TagB>().ToAction())),
                (10, Gen.Fresh(() => new RemoveTag<TagA>().ToAction())),
                (10, Gen.Fresh(() => new RemoveTag<TagB>().ToAction())),
                (3, Gen.Fresh(() => new ClearTag<TagA>().ToAction())),
                (3, Gen.Fresh(() => new ClearTag<TagB>().ToAction())),

                // Group
                (3, Gen.Fresh(() => new GetGroup<Read<ComponentA>>().ToAction())),
                (3, Gen.Fresh(() => new GetGroup<All<Read<ComponentB>, Write<IndexedComponentA>>>().ToAction())),
                (3, Gen.Fresh(() => new GetGroup<Maybe<Read<ComponentA>>>().ToAction())),
                (3, Gen.Fresh(() => new GetGroup<Read<IndexedComponentA>>(Query.From(typeof(TagA))).ToAction())),
                (3, Gen.Fresh(() => new GetGroup<Entity>(Query.Not(Query.From(typeof(TagB), typeof(SegmentA)))).ToAction())),
                (3, Gen.Fresh(() => new GetGroup<Any<Entity, Read<IndexedComponentB>>>().ToAction())),

                // Message
                (1, Gen.Fresh(() => new EmitMessage<MessageA>().ToAction())),
                (1, Gen.Fresh(() => new EmitMessage<MessageB>().ToAction()))

            // Add non generic component/tag actions

            // Add injector tests
            // Check if world.Injectors.Inject can inject all injectable types: 
            // Entities, Components, Components<T>, Tags, Tags<T>, Emitter<T>, Receiver<T>, Reaction<T>, Group<T>, Query<T>, Resource<T>, ISystem
            );
            var sequence = generator.ToSequence(
                seed => (new World(), new Model(seed)),
                (world, model) =>
                    (world.Entities().Count() == model.Entities.Count).Label("Entities.Count")
                    .And(world.Entities().All(model.Entities.Contains).Label("model.Entities.Contains()"))
                    .And(model.Entities.All(world.Entities().Has)).Label("world.Entities().Has()")
                    .And(world.Entities().Distinct().SequenceEqual(world.Entities())).Label("world.Entities().Distinct()")

                    .And(world.Entities().All(entity => world.Components().Get(entity).Count() == model.Components[entity].Count)
                        .Label("world.Components().Get().Count()"))
                    .And(world.Entities().All(entity => world.Components().Get(entity).All(component => model.Components[entity].ContainsKey(component.GetType())))
                        .Label("model.Components.ContainsKey()"))
                    .And(model.Components.All(pair => pair.Value.Keys.All(type => world.Components().Has(pair.Key, type)))
                        .Label("world.Components().Has()"))

                    .And(world.Entities().All(entity => world.Tags().Get(entity).Count() == model.Tags[entity].Count)
                        .Label("world.Tags().Get().Count()"))
                    .And(world.Entities().All(entity => world.Tags().Get(entity).All(type => model.Tags[entity].Contains(type)))
                        .Label("model.Tags.Contains()"))
                    .And(model.Tags.All(pair => pair.Value.All(type => world.Tags().Has(pair.Key, type)))
                        .Label("world.Tags().Has()"))

                    .And((world.Groups().Count == model.Groups.Count).Label("Groups.Count"))
                    .And((world.Groups().Count() == model.Groups.Count).Label("Groups.Count()"))
                    .And((world.Groups().Count <= 6).Label("world.Groups().Count <= 6"))
                    .And(world.Entities().All(entity => world.Groups().All(group => group.Fits(entity) == group.Has(entity)))
                        .Label("world.Groups().Fits()")));

            sequence.ToArbitrary().ToProperty().Check("Tests", Environment.ProcessorCount, count: count, size: size, @throw: false);

            // var restart = false;
            // while (true)
            // {
            // 	Console.WriteLine();
            // 	Console.WriteLine($"Press '{ConsoleKey.R}' to restart, '{ConsoleKey.X}' to exit.");

            // 	var key = Console.ReadKey();
            // 	if (key.Key == ConsoleKey.R)
            // 	{
            // 		restart = true;
            // 		break;
            // 	}
            // 	else if (key.Key == ConsoleKey.X) break;
            // }

            // if (restart) Run(count, size);
        }
    }
}
