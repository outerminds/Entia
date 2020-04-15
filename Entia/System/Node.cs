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
    // Could add 'When<TReact>.Parallel.RunEach' and 'When<TReact, TReceive>.Parallel.RunEach'

    public sealed partial class Node
    {
        public static partial class When<TReact> where TReact : struct, IMessage
        {
            public static partial class Receive<TReceive> where TReceive : struct, IMessage
            {
                public static Node Run(InAction<TReact, TReceive> run, int? capacity = null) => Node.Schedule(world =>
                {
                    var receiver = world.Messages().Receiver<TReceive>(capacity);
                    return Schedule((in TReact react) => { while (receiver.TryMessage(out var receive)) run(react, receive); });
                });
                public static Node Run(InAction<TReceive> run, int? capacity = null) => Run((in TReact _, in TReceive receive) => run(receive), capacity);
                public static Node Run(Action run, int? capacity = null) => Run((in TReact _, in TReceive __) => run(), capacity);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static Node RunEach(Func<Segment, InAction<TReact, TReceive>> provide, Filter filter, int? capacity = null, params IDependency[] dependencies) => Node.Schedule(world =>
                {
                    var components = world.Components();
                    var receiver = world.Messages().Receiver<TReceive>(capacity);
                    var run = new InAction<TReact, TReceive>((in TReact _, in TReceive __) => { });
                    var index = 0;
                    return Schedule((in TReact react) =>
                    {
                        while (index < components.Segments.Length)
                        {
                            var segment = components.Segments[index++];
                            if (filter.Matches(segment)) run += provide(segment);
                        }

                        while (receiver.TryMessage(out var receive)) run(react, receive);
                    }, dependencies.Prepend(new Read(typeof(Entity))));
                });

                static Result<Runner> Schedule(InAction<TReact> run, params IDependency[] dependencies) =>
                    Runner.From(run, dependencies.Prepend(new React(typeof(TReact)), new Read(typeof(TReceive))));
            }

            public static Node Run(InAction<TReact> run) => Node.Schedule(_ => Schedule(run));
            public static Node Run(Action run) => Run((in TReact _) => run());

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static Node RunEach(Func<Segment, InAction<TReact>> provide, Filter filter, params IDependency[] dependencies) => Node.Schedule(world =>
            {
                var components = world.Components();
                var run = new InAction<TReact>((in TReact _) => { });
                var index = 0;
                return Schedule((in TReact message) =>
                {
                    while (index < components.Segments.Length)
                    {
                        var segment = components.Segments[index++];
                        if (filter.Matches(segment)) run += provide(segment);
                    }
                    run(message);
                }, dependencies.Prepend(new Read(typeof(Entity))));
            });

            static Result<Runner> Schedule(InAction<TReact> run, params IDependency[] dependencies) =>
                Runner.From(run, dependencies.Prepend(new React(typeof(TReact))));
        }

        public static Node From(INode data, params Node[] children) => new Node(data, children);
        public static Node With(Func<Node> provide) => Lazy(_ => provide());
        public static Node Sequence(params Node[] children) => From(new Sequence(), children);
        public static Node Sequence(params Func<Node>[] children) => Sequence(children.Select(With));
        public static Node Parallel(params Node[] children) => From(new Parallel(), children);
        public static Node Parallel(params Func<Node>[] children) => Parallel(children.Select(With));
        public static Node Schedule(Func<World, Result<Runner>> schedule) => From(new Schedule(schedule));
        public static Node Lazy(Func<World, Result<Node>> provide) => From(new Lazy(provide));

        public readonly INode Data;
        public readonly Node[] Children;

        Node(INode data, params Node[] children)
        {
            Data = data;
            Children = children;
        }

        public Node With(INode data) => new Node(data, Children);
        public Node With(params Node[] children) => new Node(Data, children);
    }
}