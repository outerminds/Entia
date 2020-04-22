using Entia.Core;
using Entia.Experimental.Injectables;
using Entia.Injectables;
using Entia.Modules;
using Entia.Modules.Family;
using Entia.Queryables;
using FsCheck;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public struct MessageC<T> : IMessage { public T[] Values; }
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
        public unsafe struct QueryA : Queryables.IQueryable
        {
            public ComponentA* P1;
            public Entity Entity;
            public ComponentB* P2;
            public Read<ComponentB> A;
            public ComponentB* P3;
        }
        public struct QueryB : Queryables.IQueryable
        {
            public All<Read<ComponentB>, Write<ComponentA>> A;
            public Maybe<Write<ComponentC<Unit>>> B;
        }
        public unsafe struct QueryC : Queryables.IQueryable
        {
            public ComponentA* P1;
            public ComponentB* P2;
            public ComponentB* P3;
            public QueryA A;
            public ComponentB* P4;
            public ComponentA* P5;
            public ComponentB* P6;
            public Entity Entity;
            public Any<Read<ComponentB>, Write<ComponentA>> B;
            public ComponentA* P7;
            public ComponentB* P8;
        }

        public struct Injectable : IInjectable
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
            public readonly Receiver<MessageC<string>> ReceiverC;
            public readonly Defer Defer;
            public readonly Defer.Components DeferComponents;
            public readonly Defer.Entities DeferEntities;
            [All(typeof(IComponentA))]
            [None(typeof(ComponentB))]
            public readonly Group<QueryA> GroupA;
            [None(typeof(ComponentA), typeof(ComponentB))]
            public readonly Group<QueryB> GroupB;
            [All(typeof(ComponentC<>))]
            public readonly Group<QueryC> GroupC;
            public readonly Resource<ResourceA> ResourceA;
            public readonly Resource<ResourceB>.Read ResourceB;
        }

        public static void Run(int count = 8192, int size = 512, int? seed = null)
        {
            Console.Clear();

            var generator = Generator.Frequency(
            #region World
                (10, Gen.Fresh(() => new ResolveWorld().ToAction())),
            #endregion

            #region Entities
                (100, Gen.Fresh(() => new CreateEntity().ToAction())),
                (5, Gen.Fresh(() => new DestroyEntity().ToAction())),
                (1, Gen.Fresh(() => new ClearEntities().ToAction())),
            #endregion

            #region Components
                (20, Gen.Fresh(() => new AddComponent<ComponentA, IComponentA>(typeof(IComponentC)).ToAction())),
                (20, Gen.Fresh(() => new AddComponent<ComponentB, IComponentC>(typeof(IComponentA)).ToAction())),
                (20, Gen.Fresh(() => new AddComponent<ComponentC<Unit>, IComponentB>(typeof(ComponentC<>)).ToAction())),
                (20, Gen.Fresh(() => new AddComponent<ComponentC<int>, IComponentB>(typeof(ComponentC<>)).ToAction())),

                (15, Gen.Fresh(() => new RemoveComponent<ComponentA>().ToAction())),
                (15, Gen.Fresh(() => new RemoveComponent<ComponentB>().ToAction())),
                (15, Gen.Fresh(() => new RemoveComponent<ComponentC<Unit>>().ToAction())),
                (15, Gen.Fresh(() => new RemoveComponent<ComponentC<int>>().ToAction())),
                (15, Gen.Fresh(() => new RemoveComponent(typeof(IComponentA)).ToAction())),
                (15, Gen.Fresh(() => new RemoveComponent(typeof(IComponentB)).ToAction())),
                (15, Gen.Fresh(() => new RemoveComponent(typeof(IComponentC)).ToAction())),
                (15, Gen.Fresh(() => new RemoveComponent(typeof(ComponentC<>)).ToAction())),

                (5, Gen.Fresh(() => new EnableComponent<ComponentA>().ToAction())),
                (5, Gen.Fresh(() => new EnableComponent<ComponentB>().ToAction())),
                (5, Gen.Fresh(() => new EnableComponent<ComponentC<Unit>>().ToAction())),
                (5, Gen.Fresh(() => new EnableComponent<ComponentC<int>>().ToAction())),
                (5, Gen.Fresh(() => new EnableComponent(typeof(IComponentA)).ToAction())),
                (5, Gen.Fresh(() => new EnableComponent(typeof(IComponentB)).ToAction())),
                (5, Gen.Fresh(() => new EnableComponent(typeof(IComponentC)).ToAction())),
                (5, Gen.Fresh(() => new EnableComponent(typeof(ComponentC<>)).ToAction())),

                (5, Gen.Fresh(() => new DisableComponent<ComponentA>().ToAction())),
                (5, Gen.Fresh(() => new DisableComponent<ComponentB>().ToAction())),
                (5, Gen.Fresh(() => new DisableComponent<ComponentC<Unit>>().ToAction())),
                (5, Gen.Fresh(() => new DisableComponent<ComponentC<int>>().ToAction())),
                (5, Gen.Fresh(() => new DisableComponent(typeof(IComponentA)).ToAction())),
                (5, Gen.Fresh(() => new DisableComponent(typeof(IComponentB)).ToAction())),
                (5, Gen.Fresh(() => new DisableComponent(typeof(IComponentC)).ToAction())),
                (5, Gen.Fresh(() => new DisableComponent(typeof(ComponentC<>)).ToAction())),

                (4, Gen.Fresh(() => new CopyComponents().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<ComponentA>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<ComponentB>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<ComponentC<Unit>>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<ComponentC<int>>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<IComponentA>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<IComponentB>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<IComponentC>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent<IComponent>().ToAction())),
                (2, Gen.Fresh(() => new CopyComponent(typeof(ComponentB)).ToAction())),
                (2, Gen.Fresh(() => new CopyComponent(typeof(ComponentC<>)).ToAction())),
                (2, Gen.Fresh(() => new CopyComponent(typeof(IComponentC)).ToAction())),
                (2, Gen.Fresh(() => new CopyComponent(typeof(IComponent)).ToAction())),

                (5, Gen.Fresh(() => new TrimComponents().ToAction())),

                (1, Gen.Fresh(() => new ClearComponent<ComponentA>().ToAction())),
                (1, Gen.Fresh(() => new ClearComponent<ComponentB>().ToAction())),
                (1, Gen.Fresh(() => new ClearComponent<ComponentC<Unit>>().ToAction())),
                (1, Gen.Fresh(() => new ClearComponent<ComponentC<int>>().ToAction())),
                (1, Gen.Fresh(() => new ClearComponent(typeof(IComponentA)).ToAction())),
                (1, Gen.Fresh(() => new ClearComponent(typeof(IComponentB)).ToAction())),
                (1, Gen.Fresh(() => new ClearComponent(typeof(IComponentC)).ToAction())),
                (1, Gen.Fresh(() => new ClearComponent(typeof(ComponentC<>)).ToAction())),
                (1, Gen.Fresh(() => new ClearComponents().ToAction())),
            #endregion

            #region Families
                (25, Gen.Fresh(() => new AdoptEntity().ToAction())),
                (25, Gen.Fresh(() => new RejectEntity().ToAction())),
            #endregion

            #region Groups
                (1, Gen.Fresh(() => new GetGroup<Read<ComponentA>>().ToAction())),
                (1, Gen.Fresh(() => new GetGroup<All<Read<ComponentB>, Write<ComponentC<Unit>>>>().ToAction())),
                (1, Gen.Fresh(() => new GetGroup<Maybe<Read<ComponentA>>>().ToAction())),
                (1, Gen.Fresh(() => new GetGroup<Read<ComponentC<int>>>(typeof(ProviderA)).ToAction())),
                (1, Gen.Fresh(() => new GetGroup<QueryA>().ToAction())),
                (1, Gen.Fresh(() => new GetGroup<QueryB>().ToAction())),
                (1, Gen.Fresh(() => new GetGroup<QueryC>().ToAction())),
                (1, Gen.Fresh(() => new GetEntityGroup(typeof(ProviderB)).ToAction())),
                (1, Gen.Fresh(() => new GetEntityGroup(typeof(ProviderC)).ToAction())),
                (1, Gen.Fresh(() => new GetPointerGroup(typeof(ProviderA)).ToAction())),
                (1, Gen.Fresh(() => new GetPointerGroup().ToAction())),
                (1, Gen.Fresh(() => new GetGroup<Any<Write<ComponentC<Unit>>, Read<ComponentB>>>().ToAction())),
            #endregion

            #region Messages
                (1, Gen.Fresh(() => new EmitMessage<MessageA>().ToAction())),
                (1, Gen.Fresh(() => new EmitMessage<MessageB>().ToAction())),
                (1, Gen.Fresh(() => new EmitMessage<MessageC<string>>().ToAction())),
            #endregion

            #region Injectables
                (1, Gen.Fresh(() => new Inject(_injectables).ToAction())),
                (1, Gen.Fresh(() => new Inject<Components<ComponentA>>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Components<ComponentB>.Read>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Components<ComponentC<int>>.Write>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Emitter<MessageA>>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Reaction<MessageB>>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Receiver<MessageC<string>>>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Group<Entity>>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Resource<ResourceA>>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Resource<ResourceB>.Read>().ToAction())),
                (1, Gen.Fresh(() => new Inject<Injectable>().ToAction())),
            #endregion

            #region Systems
                (2, Gen.Fresh(() => new RunSystem<MessageA, MessageB>().ToAction())),
                (2, Gen.Fresh(() => new RunSystem<MessageB, MessageC<Unit>>().ToAction())),
                (2, Gen.Fresh(() => new RunSystem<MessageC<int>, MessageC<uint>>().ToAction())),
                (2, Gen.Fresh(() => new RunEachSystem<MessageA, MessageB, ComponentA, ComponentB>().ToAction())),
                (2, Gen.Fresh(() => new RunEachSystem<MessageB, MessageC<Unit>, ComponentB, ComponentC<Unit>>().ToAction())),
                (2, Gen.Fresh(() => new RunEachSystem<MessageC<int>, MessageC<uint>, ComponentC<Unit>, ComponentC<int>>().ToAction())),
            #endregion

            #region Queryables
                (1, Gen.Fresh(() => new Query(_queryables).ToAction())),
            #endregion

            #region Resolvables
                (5, Gen.Fresh(() => new Resolve().ToAction()))
            #endregion

            // Add non generic component actions
            );
            var sequence = generator.ToSequence(
                random => (new World(), new Model(random)),
                (world, model) => PropertyUtility.All(Tests(world, model)));

            IEnumerable<(bool test, string label)> Tests(World world, Model model)
            {
                var entities = world.Entities();
                var families = world.Families();
                var components = world.Components();

                #region World
                yield return (World.Instances().Contains(world), "World.Instances().Contains()");
                yield return (World.TryInstance(world.Equals, out _), "World.TryInstance");
                #endregion

                #region Entities
                yield return (entities.Count == model.Entities.Count, "Entities.Count");
                yield return (entities.All(_ => _), "Entities.All()");
                yield return (entities.All(model.Entities.Contains), "model.Entities.Contains()");
                yield return (model.Entities.All(entities.Has), "entities.Has()");
                yield return (entities.Distinct().SequenceEqual(entities), "entities.Distinct()");
                #endregion

                #region Components
                IEnumerable<(bool test, string label)> WithInclude(States include)
                {
                    yield return (components.Get(include).Count() == components.Count(include), $"Components.Get({include}).Count() == Components.Count({include})");
                    yield return (components.Get(include).Any() == components.Has(include), $"Components.Get({include}).Any() == Components.Has({include})");
                    yield return (components.Count(include) > 0 == components.Has(include), $"Components.Count({include}) > 0 == Components.Has({include})");

                    yield return (components.Count<IComponent>(include) >= entities.Count(entity => components.Has<IComponent>(entity, include)), $"components.Count(IComponent, {include}) >= entities.Components");
                    yield return (components.Count(typeof(IComponent), include) >= entities.Count(entity => components.Has(entity, typeof(IComponent), include)), $"components.Count(IComponent, {include}) >= entities.Components");
                    yield return (components.Count(include) == components.Count<IComponent>(include), $"components.Count({include}) == components.Count<IComponent>({include})");
                    yield return (components.Count<IComponent>(include) == components.Count(typeof(IComponent), include), $"Components.Count<IComponent>({include}) == Components.Count(IComponent, {include})");

                    yield return (entities.All(entity => components.Get(entity, include).All(component => components.Has(entity, component.GetType(), include))), $"entities.All(components.Has({include}))");
                    yield return (entities.All(entity => components.Get(entity, include).All(component => components.TryGet(entity, component.GetType(), out _, include))), $"entities.All(components.TryGet({include}))");
                    yield return (entities.All(entity => components.Get(entity, include).Count() == components.Count(entity, include)), $"entities.All(components.Get({include}).Count() == components.Count(Enabled))");
                    yield return (entities.Sum(entity => components.Count(entity, include)) == components.Count(include), $"entities.Sum({include}) == components.Count({include})");
                }

                foreach (var pair in WithInclude(States.All)) yield return pair;
                foreach (var pair in WithInclude(States.None)) yield return pair;
                foreach (var pair in WithInclude(States.Enabled)) yield return pair;
                foreach (var pair in WithInclude(States.Disabled)) yield return pair;

                yield return (entities.All(entity => components.Get(entity, States.Enabled).None(component => components.Enable(entity, component.GetType()))), "entities.None(components.Enable())");
                yield return (entities.All(entity => components.Get(entity, States.Disabled).None(component => components.Disable(entity, component.GetType()))), "entities.None(components.Disable())");
                yield return (entities.All(entity => components.Get(entity).Count() == model.Components[entity].Count), "components.Get(entity).Count()");
                yield return (entities.All(entity => components.Count(entity) == model.Components[entity].Count), "components.Count(entity)");
                yield return (entities.All(entity => components.Get(entity).All(component => model.Components[entity].Contains(component.GetType()))), "model.Components.ContainsKey()");

                yield return (model.Components.All(pair => pair.Value.All(type => components.Has(pair.Key, type))), "components.Has()");
                #endregion

                #region Families
                var allFamilies = families.Roots().SelectMany(root => families.Family(root, From.Top)).ToArray();
                yield return (entities.All(entity => families.Parent(entity) || families.Roots().Contains(entity)), "entities.All(Parent(entity) || families.Roots().Contains(entity))");
                yield return (families.Roots().None(root => families.Parent(root)), "families.Roots().None(Parent(root))");
                yield return (entities.All(entity => families.Parent(entity) == families.Ancestors(entity).FirstOrDefault()), "entities.All(families.Parent(entity) == families.Ancestors(entity).First())");
                yield return (allFamilies.Except(entities).None(), "allFamilies.Except(entities).None()");
                yield return (allFamilies.Distinct().SequenceEqual(allFamilies), "allFamilies.Distinct().SequenceEquals(allFamilies)");
                yield return (entities.All(entity => families.Siblings(entity).All(sibling => families.Parent(sibling) == families.Parent(entity))), "families.Siblings(entity).All(Parent(sibling) == Parent(entity))");
                yield return (entities.All(entity => families.Family(entity).Contains(entity)), "families.Family(entity).Contains(entity)");
                #endregion

                #region Groups
                yield return (world.Groups().Count <= 30, "world.Groups().Count <= 30");
                #endregion
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
