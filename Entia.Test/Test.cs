using Entia.Core;
using Entia.Injectables;
using Entia.Modules;
using Entia.Modules.Query;
using Entia.Queryables;
using Entia.Systems;
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
        static readonly Type[] _injectables;
        static readonly Type[] _queryables;
        static readonly Type[] _components;
        static readonly Type[] _resources;

        static Test()
        {
            var injectables = new List<Type>();
            var queryables = new List<Type>();
            var components = new List<Type>();
            var resources = new List<Type>();

            foreach (var type in TypeUtility.AllTypes.Where(type => !type.IsAbstract && !type.IsGenericType))
            {
                if (type.Is<IInjectable>()) injectables.Add(type);
                if (type.Is<Queryables.IQueryable>()) queryables.Add(type);
                if (type.Is<IComponent>()) components.Add(type);
                if (type.Is<IResource>()) resources.Add(type);
            }

            _injectables = injectables.ToArray();
            _queryables = queryables.ToArray();
            _components = components.ToArray();
            _resources = resources.ToArray();
        }

        public interface IComponentA : IComponentC { }
        public interface IComponentB : IComponentC { }
        public interface IComponentC : IComponent { }
        public struct ComponentA : IComponentA { }
        public struct ComponentB : IComponentA { public float Value; }
        public struct ComponentC<T> : IComponentB { public List<T> A, B, C; }
        public struct MessageA : IMessage { }
        public struct MessageB : IMessage { public ulong A, B; }
        public struct MessageC : IMessage { public string[] Values; }
        public struct ResourceA : IResource { }
        public struct ResourceB : IResource { public byte A, B, C; }
        public struct ResourceC : IResource { public Stack<int> Values; }
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

        public struct Injectable : ISystem
        {
            public readonly World World;
            public readonly AllEntities Entities;
            public readonly AllEntities.Read EntitiesRead;
            public readonly AllComponents Components;
            public readonly AllComponents.Read ComponentsRead;
            public readonly AllComponents.Write ComponentsWrite;
            public readonly Components<ComponentA> ComponentsA;
            public readonly Components<ComponentB>.Read ComponentsB;
            public readonly Components<ComponentC<Unit>>.Write ComponentsC;
            public readonly AllEmitters Emitters;
            public readonly Emitter<MessageA> EmitterA;
            public readonly Reaction<MessageB> ReactionB;
            public readonly Receiver<MessageC> ReceiverC;
            public readonly Defer Defer;
            [All(typeof(IComponentA))]
            [None(typeof(ComponentB))]
            public readonly Group<QueryA> GroupA;
            [None(typeof(ComponentA), typeof(ComponentB))]
            public readonly Group<QueryB> GroupB;
            [All(typeof(ComponentC<>))]
            public readonly Group<QueryC> GroupC;
            public readonly Resource<ResourceA> ResourceA;
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
                (1, Gen.Fresh(() => new ClearComponent<ComponentA>().ToAction())),
                (1, Gen.Fresh(() => new ClearComponent<ComponentB>().ToAction())),
                (1, Gen.Fresh(() => new ClearComponent<ComponentC<Unit>>().ToAction())),
                (1, Gen.Fresh(() => new ClearComponent(typeof(IComponentB)).ToAction())),

                // Group
                (1, Gen.Fresh(() => new GetGroup<Read<ComponentA>>().ToAction())),
                (1, Gen.Fresh(() => new GetGroup<All<Read<ComponentB>, Write<ComponentC<Unit>>>>().ToAction())),
                (1, Gen.Fresh(() => new GetGroup<Maybe<Read<ComponentA>>>().ToAction())),
                (1, Gen.Fresh(() => new GetGroup<Read<ComponentC<Unit>>>(typeof(ProviderA)).ToAction())),
                (1, Gen.Fresh(() => new GetGroup<QueryA>().ToAction())),
                (1, Gen.Fresh(() => new GetGroup<QueryB>().ToAction())),
                (1, Gen.Fresh(() => new GetGroup<QueryC>().ToAction())),
                (1, Gen.Fresh(() => new GetEntityGroup(typeof(ProviderB)).ToAction())),
                (1, Gen.Fresh(() => new GetEntityGroup(typeof(ProviderC)).ToAction())),
                (1, Gen.Fresh(() => new GetGroup<Any<Write<ComponentC<Unit>>, Read<ComponentB>>>().ToAction())),

                // Message
                (1, Gen.Fresh(() => new EmitMessage<MessageA>().ToAction())),
                (1, Gen.Fresh(() => new EmitMessage<MessageB>().ToAction())),
                (1, Gen.Fresh(() => new EmitMessage<MessageC>().ToAction())),

                // Injectables
                (1, Gen.Fresh(() => new Inject(_injectables).ToAction())),
                (1, Gen.Fresh(() => new Inject<Components<ComponentA>>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Components<ComponentB>.Read>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Components<ComponentC<Unit>>.Write>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Emitter<MessageA>>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Reaction<MessageB>>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Receiver<MessageC>>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Group<Entity>>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Resource<ResourceA>>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Resource<ResourceB>.Read>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Injectable>().ToAction())),

                // Queryables
                (1, Gen.Fresh(() => new Query(_queryables).ToAction())),

                // Resolvables
                (1, Gen.Fresh(() => new Resolve().ToAction()))

            // Add non generic component actions
            // Add resolver tests
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
                    .And((world.Groups().Count <= 15).Label("world.Groups().Count <= 15")));

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
