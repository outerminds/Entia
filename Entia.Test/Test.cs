using Entia.Core;
using Entia.Modules;
using Entia.Modules.Query;
using Entia.Queryables;
using FsCheck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Entia.Test
{
    public static class Test
    {
        public interface IComponentA : IComponentC { }
        public interface IComponentB : IComponentC { }
        public interface IComponentC : IComponent { }
        public struct ComponentA : IComponentA { }
        public struct ComponentB : IComponentA { public float Value; }
        public struct ComponentC<T> : IComponentB { public List<T> A, B, C; }
        public struct MessageA : IMessage { }
        public struct MessageB : IMessage { }
        [All(typeof(ComponentC<>))]
        public struct ProviderA { }
        [None(typeof(ComponentA), typeof(ComponentB))]
        public struct ProviderB { }
        [All(typeof(IComponentA))]
        [None(typeof(ComponentB))]
        public struct ProviderC { }
        public struct QueryA : Queryables.IQueryable
        {
            public Entity Entity;
            public Read<ComponentB> A;
        }
        public struct QueryB : Queryables.IQueryable
        {
            public All<Read<ComponentB>, Write<ComponentA>> A;
            public Maybe<Write<ComponentC<Unit>>> B;
        }
        public struct QueryC : Queryables.IQueryable
        {
            public QueryA A;
            public Any<Read<ComponentB>, Write<ComponentA>> B;
        }

        public static void Run(int count = 1600, int size = 1600)
        {
            Console.Clear();

            var generator = Generator.Frequency(
                // World
                (10, Gen.Fresh(() => new ResolveWorld().ToAction())),

                // Entity
                (100, Gen.Fresh(() => new CreateEntity().ToAction())),
                (10, Gen.Fresh(() => new DestroyEntity().ToAction())),
                (1, Gen.Fresh(() => new ClearEntities().ToAction())),

                // Component
                (20, Gen.Fresh(() => new AddComponent<ComponentA, IComponentA>(typeof(IComponentC)).ToAction())),
                (20, Gen.Fresh(() => new AddComponent<ComponentB, IComponentC>(typeof(IComponentA)).ToAction())),
                (20, Gen.Fresh(() => new AddComponent<ComponentC<Unit>, IComponentB>(typeof(ComponentC<>)).ToAction())),
                (15, Gen.Fresh(() => new RemoveComponent<ComponentA>().ToAction())),
                (15, Gen.Fresh(() => new RemoveComponent<ComponentB>().ToAction())),
                (15, Gen.Fresh(() => new RemoveComponent<ComponentC<Unit>>().ToAction())),
                (15, Gen.Fresh(() => new RemoveComponent(typeof(IComponentC)).ToAction())),
                (15, Gen.Fresh(() => new RemoveComponent(typeof(ComponentC<>)).ToAction())),
                (2, Gen.Fresh(() => new ClearComponent<ComponentA>().ToAction())),
                (2, Gen.Fresh(() => new ClearComponent<ComponentB>().ToAction())),
                (2, Gen.Fresh(() => new ClearComponent<ComponentC<Unit>>().ToAction())),
                (2, Gen.Fresh(() => new ClearComponent(typeof(IComponentB)).ToAction())),

                // Group
                (3, Gen.Fresh(() => new GetGroup<Read<ComponentA>>().ToAction())),
                (3, Gen.Fresh(() => new GetGroup<All<Read<ComponentB>, Write<ComponentC<Unit>>>>().ToAction())),
                (3, Gen.Fresh(() => new GetGroup<Maybe<Read<ComponentA>>>().ToAction())),
                (3, Gen.Fresh(() => new GetGroup<Read<ComponentC<Unit>>>(typeof(ProviderA)).ToAction())),
                (3, Gen.Fresh(() => new GetGroup<QueryA>().ToAction())),
                (3, Gen.Fresh(() => new GetGroup<QueryB>().ToAction())),
                (3, Gen.Fresh(() => new GetGroup<QueryC>().ToAction())),
                (3, Gen.Fresh(() => new GetEntityGroup(typeof(ProviderB)).ToAction())),
                (3, Gen.Fresh(() => new GetEntityGroup(typeof(ProviderC)).ToAction())),
                (3, Gen.Fresh(() => new GetGroup<Any<Write<ComponentC<Unit>>, Read<ComponentB>>>().ToAction())),

                // Message
                (5, Gen.Fresh(() => new EmitMessage<MessageA>().ToAction())),
                (5, Gen.Fresh(() => new EmitMessage<MessageB>().ToAction()))

            // Add non generic component actions
            // Add injector tests
            // Add resolver tests
            // Check if world.Injectors.Inject can inject all injectable types: 
            // AllEntities, AllComponents, Components<T>, Emitter<T>, Receiver<T>, Reaction<T>, Group<T>, Query<T>, Resource<T>, ISystem
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

                    .And((world.Groups().Count == model.Groups.Count).Label("Groups.Count"))
                    .And((world.Groups().Count() == model.Groups.Count).Label("Groups.Count()"))
                    .And((world.Groups().Count <= 10).Label("world.Groups().Count <= 10")));

            var parallel =
#if DEBUG
            1;
#else
            Environment.ProcessorCount;
#endif
            var result = sequence.ToArbitrary().ToProperty().Check("Tests", parallel, count: count, size: size);

            if (!result.success)
            {
                while (true)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Press '{ConsoleKey.R}' to restart, '{ConsoleKey.O}' to replay orginal, '{ConsoleKey.P}' to replay shrunk, '{ConsoleKey.X}' to exit.");

                    var key = Console.ReadKey();
                    Console.WriteLine();
                    switch (key.Key)
                    {
                        case ConsoleKey.R: Run(count, size); return;
                        case ConsoleKey.O: result.original.ToProperty(result.seed).QuickCheck("Replay Original"); break;
                        case ConsoleKey.P: result.shrunk.ToProperty(result.seed).QuickCheck("Replay Shrunk"); break;
                        case ConsoleKey.X: return;
                    }
                }
            }
        }
    }
}
