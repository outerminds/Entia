using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Linq;
using Entia.Core;

namespace Entia.Experiment.Next
{
    public interface IComponent<T> : IComponent { }
    public sealed class Position : IComponent<Vector2> { }

    public readonly struct Component
    {
        static int _counter;

        public static Component<T> Create<T>(Func<T> @default = null) =>
            new Component<T>(Interlocked.Increment(ref _counter), @default);
        public static Component<T> Create<T>(T @default) => Create(() => @default);
        public static Component<Unit> Create() => Create<Unit>();

        public readonly Type Name;
        public readonly Type Data;
    }

    public readonly struct Component<T>
    {
        public static implicit operator Component(in Component<T> component) => throw null;

        public readonly int Index;
        public readonly Func<T> Default;

        public Component(int index, Func<T> @default = null)
        {
            Index = index;
            Default = @default ?? (() => default);
        }
    }

    public readonly struct Component<TName, TData>
    {
        public static implicit operator Component(in Component<TName, TData> component) => throw null;
    }

    public readonly struct Read<T>
    {
        public ref readonly T Value => throw null;
    }
    public readonly struct Write<T>
    {
        public ref T Value => throw null;
    }
    public readonly struct Maybe<T>
    {
        public readonly bool Has;
        public readonly T Value;
    }
    public readonly struct All<T1, T2>
    {
        public readonly T1 Value1;
        public readonly T2 Value2;
    }
    public readonly struct Any<T1, T2>
    {
        public readonly Maybe<T1> Value1;
        public readonly Maybe<T2> Value2;
    }
    public readonly struct Query<T> { }

    public static class Query
    {
        public static Query<Read<T>> Read<T>(Component<T> component) => throw null;
        public static Query<Write<T>> Write<T>(Component<T> component) => throw null;
        public static Query<Maybe<T>> Maybe<T>(Query<T> query) => throw null;
        public static Query<All<T1, T2>> All<T1, T2>(Query<T1> query1, Query<T2> query2) => throw null;
        public static Query<Any<T1, T2>> Any<T1, T2>(Query<T1> query1, Query<T2> query2) => throw null;
    }

    public sealed class Group<T> : IEnumerable<T>
    {
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public sealed class World
    {
        public readonly Modules.Entities Entities = new Modules.Entities();
        public readonly Modules.Components Components = new Modules.Components();
        public readonly Modules.Groups Groups = new Modules.Groups();
        public readonly Modules.Controllers Controllers = new Modules.Controllers();
    }

    public static class Phase
    {
        static int _counter = -1;

        public static Phase<T> Create<T>() => new Phase<T>(Interlocked.Increment(ref _counter));
        public static Phase<Unit> Create() => Create<Unit>();
    }

    public readonly struct Phase<T>
    {
        public readonly int Index;
        public Phase(int index) { Index = index; }
    }

    public sealed class Execution
    {
        public static Execution Create<T>(Phase<T> phase, Action run) => throw null;
        public static Execution Create<T>(Phase<T> phase, Action<T> run) => throw null;
        public static Execution Create<T>(Phase<T> phase, InAction<T> run) => throw null;

        Execution() { }
    }

    public sealed class System
    {
        public readonly Execution[] Executions;

        public System(params Execution[] executions)
        {
            Executions = executions;
        }
    }

    public sealed class Controller
    {
        public bool Run(Phase<Unit> phase) => throw null;
        public bool Run<T>(Phase<T> phase, in T data) => throw null;
    }

    public sealed class Node
    {
        public static Node System(Func<World, System> provider) => throw null;
        public static Node System(Func<World, Execution> provider) =>
            System(world => new System(provider(world)));
        public static Node System(Func<World, Execution[]> provider) =>
            System(world => new System(provider(world)));
        public static Node System(Func<World, IEnumerable<Execution>> provider) =>
            System(world => new System(provider(world).ToArray()));
        public static Node Sequence(params Node[] nodes) => throw null;
    }

    namespace Modules
    {
        public sealed class Entities
        {
            public Entity Create() => throw null;
            public bool Destroy(Entity entity) => throw null;
        }

        public sealed class Components
        {
            public ref TData Get<TName, TData>(Entity entity) => throw null;
            public ref TData Get<TName, TData>(Entity entity, in Component<TName, TData> type) => throw null;
            public ref T Get<T>(Entity entity, IComponent<T> type) => throw null;
            public ref T Get<T>(Entity entity, in Component<T> component) => throw null;
            public bool Set<T>(Entity entity, in Component<T> component, in T data) => throw null;
            public bool Remove<T>(Entity entity, in Component<T> component) => throw null;
        }

        public sealed class Groups
        {
            public Group<T> Get<T>(Query<T> query) => throw null;
        }

        public sealed class Controllers
        {
            public Controller Get(Node node) => throw null;
        }
    }

    public static partial class Components
    {
        public static readonly Component<string> Name = Component.Create("");
        public static readonly Component<Vector2> Position = Component.Create(Vector2.Zero);
        public static readonly Component<Vector2> Velocity = Component.Create(Vector2.Zero);
        public static readonly Component<(float duration, float counter)> Lifetime = Component.Create((5f, 0f));
        public static readonly Component<(Vector2 source, Vector2 target, float duration, float counter)> ScaleTo =
            Component.Create((Vector2.One, Vector2.One, 0f, 0f));

        public static readonly Component<Position, Vector2> P;

        public static partial class D2
        {
            public static readonly Component<Position, Vector2> Position;
        }

        public static partial class D3
        {
            public static readonly Component<Position, Vector3> Position;
        }
    }

    public static partial class Phases
    {
        public static readonly Phase<Unit> Initialize = Phase.Create();
        public static readonly Phase<float> Run = Phase.Create<float>();
        public static readonly Phase<Unit> Dispose = Phase.Create();

        public static class React
        {
            public static readonly Phase<Unit> Initialize = Phase.Create();
            public static readonly Phase<Unit> Dispose = Phase.Create();
        }
    }

    public static partial class Phases<T>
    {
        public static readonly Phase<T> React = Phase.Create<T>();
    }

    public static partial class Systems
    {
        public static System Motion(World world)
        {
            var query = Query.All(Query.Write(Components.Position), Query.Read(Components.Velocity));
            var group = world.Groups.Get(query);
            world.Components.Get(default, Components.D2.Position);
            world.Components.Get(default, Components.D3.Position);

            void Initialize() { }

            void Run(float delta)
            {
                foreach (var item in group)
                {
                    ref var position = ref item.Value1.Value;
                    ref readonly var velocity = ref item.Value2.Value;
                    position += velocity;
                }
            }

            void Dispose() { }

            return new System(
                Execution.Create(Phases.Initialize, Initialize),
                Execution.Create(Phases.Run, Run),
                Execution.Create(Phases.Dispose, Dispose));
        }

        public static IEnumerable<Execution> Motion2(World world)
        {
            var query = Query.All(Query.Write(Components.Position), Query.Read(Components.Velocity));
            var group = world.Groups.Get(query);
            world.Components.Get(default, Components.D2.Position);
            world.Components.Get(default, Components.D3.Position);

            yield return Execution.Create(Phases.Initialize, () => { });
            yield return Execution.Create(Phases.Dispose, () => { });
            yield return Execution.Create(Phases.Run, delta =>
            {
                foreach (var item in group)
                {
                    ref var position = ref item.Value1.Value;
                    ref readonly var velocity = ref item.Value2.Value;
                    position += velocity;
                }
            });
        }
    }

    public static partial class Nodes
    {
        public static readonly Node Main = Node.Sequence(
            Node.System(Systems.Motion),
            Node.System(Systems.Motion2)
        );
    }

    public static class Program
    {
        public static void Test()
        {
            var world = new World();
            var controller = world.Controllers.Get(Nodes.Main);
            controller.Run(Phases.Initialize);
            for (int i = 0; i < 1000; i++) controller.Run(Phases.Run, i);
            controller.Run(Phases.Dispose);
        }
    }
}