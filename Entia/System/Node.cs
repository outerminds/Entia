using System;
using Entia.Core;
using System.Linq;
using Entia.Dependencies;
using Entia.Modules;
using Entia.Modules.Component;
using System.Runtime.CompilerServices;
using Entia.Experimental.Nodes;
using Entia.Experimental.Scheduling;

namespace Entia.Experimental
{
    public sealed partial class Node
    {
        public static partial class System<TMessage> where TMessage : struct, IMessage
        {
            public static Node Run(InAction<TMessage> run) => Node.From(new Schedule(_ => CreateRunner(run)));
            public static Node Run(Action run) => Run((in TMessage _) => run());

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static Node RunEach(Func<Segment, InAction<TMessage>> provide, Filter filter, params IDependency[] dependencies) => From(new Schedule(world =>
            {
                var components = world.Components();
                var run = new InAction<TMessage>((in TMessage _) => { });
                var index = 0;
                return CreateRunner((in TMessage message) =>
                {
                    while (index < components.Segments.Length)
                    {
                        var segment = components.Segments[index++];
                        if (filter.Matches(segment)) run += provide(segment);
                    }
                    run(message);
                }, dependencies.Prepend(new Read(typeof(Entity))));
            }));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static Result<Runner> CreateRunner(InAction<TMessage> run, params IDependency[] dependencies) =>
                Runner.From(run, dependencies.Prepend(new React(typeof(TMessage))));
        }

        public static Node From(INode data, params Node[] children) => new Node(data, children);
        public static Node Lazy(Func<Node> provide) => From(new Lazy(_ => provide()));
        public static Node Sequence(params Node[] children) => From(new Sequence(), children);
        public static Node Sequence(params Func<Node>[] children) => Sequence(children.Select(Lazy));
        public static Node Parallel(params Node[] children) => From(new Parallel(), children);
        public static Node Parallel(params Func<Node>[] children) => Parallel(children.Select(Lazy));

        public readonly INode Value;
        public readonly Node[] Children;

        Node(INode data, params Node[] children)
        {
            Value = data;
            Children = children;
        }

        public Node With(INode data) => new Node(data, Children);
        public Node With(params Node[] children) => new Node(Value, children);
    }
}