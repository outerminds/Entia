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
        public struct ComponentB : IComponentA
        {
            [Default]
            public static ComponentB Default => new ComponentB { Value = 19f };
            public float Value;
        }
        public struct ComponentC<T> : IComponentB
        {
            [Default]
            public static ComponentC<T> Default => new ComponentC<T> { B = new List<T>(), C = new List<T> { default, default } };
            public List<T> A, B, C;
        }
        public struct MessageA : IMessage { }
        public struct MessageB : IMessage
        {
            [Default]
            public static MessageB Default() => new MessageB { A = 12, B = 26 };
            public ulong A, B;
        }
        public struct MessageC : IMessage { public string[] Values; }
        public struct ResourceA : IResource { }
        public struct ResourceB : IResource
        {
            [Default]
            public static readonly ResourceB Default = new ResourceB { A = 11, B = 23, C = 37 };
            public byte A, B, C;
        }
        public struct ResourceC<T> : IResource
        {
            [Default]
            public static ResourceC<T> Default => new ResourceC<T> { Values = new Stack<T>() };
            public Stack<T> Values;
        }
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

        public struct Injectable : IDispose
        {
            [Default]
            public static Injectable Default => new Injectable { _values = new List<int>() };

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
            public readonly Resource<ResourceB>.Read ResourceB;

            List<int> _values;

            public void Dispose() { _values.Clear(); }
        }

        public static void Run(int count = 1600, int size = 1600, int? seed = null)
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
                (5, Gen.Fresh(() => new TrimComponents().ToAction())),

                (10, Gen.Fresh(() => new EnableComponent<ComponentA>().ToAction())),
                (10, Gen.Fresh(() => new EnableComponent<ComponentB>().ToAction())),
                (10, Gen.Fresh(() => new EnableComponent<ComponentC<Unit>>().ToAction())),
                (10, Gen.Fresh(() => new EnableComponent(typeof(IComponentC)).ToAction())),
                (10, Gen.Fresh(() => new EnableComponent(typeof(ComponentC<>)).ToAction())),

                (10, Gen.Fresh(() => new DisableComponent<ComponentA>().ToAction())),
                (10, Gen.Fresh(() => new DisableComponent<ComponentB>().ToAction())),
                (10, Gen.Fresh(() => new DisableComponent<ComponentC<Unit>>().ToAction())),
                (10, Gen.Fresh(() => new DisableComponent(typeof(IComponentC)).ToAction())),
                (10, Gen.Fresh(() => new DisableComponent(typeof(ComponentC<>)).ToAction())),

                (4, Gen.Fresh(() => new CopyComponents().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<ComponentA>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<ComponentB>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<ComponentC<Unit>>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<IComponentA>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<IComponentB>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<IComponentC>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<IComponent>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent(typeof(ComponentB)).ToAction())),
                (2, Gen.Fresh(() => new CopyComponent(typeof(ComponentC<>)).ToAction())),
                (2, Gen.Fresh(() => new CopyComponent(typeof(IComponentC)).ToAction())),
                (2, Gen.Fresh(() => new CopyComponent(typeof(IComponent)).ToAction())),

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
            );
            var sequence = generator.ToSequence(
                random => (new World(), new Model(random)),
                (world, model) => PropertyUtility.All(Tests(world, model)));

            IEnumerable<(bool test, string label)> Tests(World world, Model model)
            {
                var entities = world.Entities();
                var components = world.Components();
                yield return (World.Instances().Contains(world), "World.Instances().Contains()");
                yield return (World.TryInstance(world.Equals, out _), "World.TryInstance");
                yield return (entities.Count == model.Entities.Count, "Entities.Count");
                yield return (entities.All(model.Entities.Contains), "model.Entities.Contains()");
                yield return (model.Entities.All(entities.Has), "entities.Has()");
                yield return (entities.Distinct().SequenceEqual(entities), "entities.Distinct()");
                yield return (entities.All(_ => _), "Entities.All()");
                yield return (entities.All(entity => components.Get(entity).Count() == model.Components[entity].Count), "components.Get().Count()");
                yield return (entities.All(entity => components.Get(entity).All(component => model.Components[entity].ContainsKey(component.GetType()))), "model.Components.ContainsKey()");
                yield return (model.Components.All(pair => pair.Value.Keys.All(type => components.Has(pair.Key, type))), "components.Has()");
                yield return (world.Groups().Count <= 15, "world.Groups().Count <= 15");
            }

            var parallel =
#if DEBUG
            1;
#else
            Environment.ProcessorCount;
#endif
            var result = sequence.ToArbitrary().ToProperty(seed).Check("Tests", parallel, count: count, size: size);

            if (!result.success)
            {
                while (true)
                {
                    Console.WriteLine();
                    Console.WriteLine($"'R' to restart, 'O' to replay orginal, 'S' to replay shrunk, 'X' to exit.");

                    var line = Console.ReadLine();
                    Console.WriteLine();
                    switch (line)
                    {
                        case "r":
                        case "R": Run(count, size); return;
                        case "o":
                        case "O": result.original.ToProperty(result.seed).QuickCheck("Replay Original"); break;
                        case "s":
                        case "S": result.shrunk.ToProperty(result.seed).QuickCheck("Replay Shrunk"); break;
                        case "x":
                        case "X": return;
                        default:
                            if (int.TryParse(line, out var random))
                            {
                                Run(count, size, random);
                                return;
                            }
                            break;
                    }
                }
            }
        }
    }
}
