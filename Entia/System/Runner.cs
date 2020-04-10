using System;
using Entia.Core;
using System.Linq;
using Entia.Dependencies;
using Entia.Modules;
using Entia.Modules.Component;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Entia.Experimental
{
    public delegate Result<Phase[]> Schedule(World world);

    public interface INode { }
    public sealed class Node
    {
        public static Node System(Runner runner) => new Node(new SystemNode(runner));
        public static Node System(Func<Runner> provide) => System(Runner.Lazy(provide));
        public static Node System(params Runner[] runners) => System(Runner.All(runners));
        public static Node System(params Func<Runner>[] runners) => System(runners.Select(Runner.Lazy));

        public readonly INode Data;
        public readonly Node[] Children;
        public Node(INode data, params Node[] children) => (Data, Children) = (data, children);
    }

    public interface IScheduler : ITrait
    {
        Result<Phase[]> Schedule(Node node, World world);
    }

    public readonly struct SystemNode : INode
    {
        public readonly Runner Runner;
        public SystemNode(Runner runner) { Runner = runner; }
    }

    public sealed class SystemBuilder : IScheduler
    {
        public Result<Phase[]> Schedule(Node node, World world) => Result.Cast<SystemNode>(node.Data)
            .Bind(system => system.Runner.Schedule(world));
    }

    public static class Boba
    {
        static void Build(Node node, World world)
        {
            IScheduler scheduler = default;
            if (scheduler.Schedule(node, world).TryValue(out var phases))
            {
                foreach (var phase in phases)
                {
                    if (Phase.TryReact(phase, world)) continue;
                    return;
                }
            }
        }
    }

    public sealed class SequenceBuilder : IScheduler
    {
        public Result<Phase[]> Schedule(Node node, World world) => node.Children
            .Select(child =>
                world.Container.TryGet<IScheduler>(child.Data.GetType(), out var scheduler) ?
                scheduler.Schedule(child, world) : Result.Failure())
            .All()
            .Map(phases => phases.Flatten()
                .GroupBy(phase => phase.Type)
                .Select(group => Phase.Combine(group.ToArray()))
                .Choose()
                .ToArray());
    }

    public sealed class ParallelBuilder : IScheduler
    {
        public Result<Phase[]> Schedule(Node node, World world) => node.Children
            .Select(child =>
                world.Container.TryGet<IScheduler>(child.Data.GetType(), out var scheduler) ?
                scheduler.Schedule(child, world) : Result.Failure())
            .All()
            .Map(phases => phases
                .Flatten()
                .GroupBy(phase => phase.Type)
                .Select(group => Phase.Combine(runs => message => Parallel.ForEach(runs, run => run(message)), group.ToArray()))
                .Choose()
                .ToArray());
    }

    public readonly partial struct Runner
    {
        public static partial class When<TReact> where TReact : struct, IMessage
        {
            public static Runner Run(InAction<TReact> run) => new Runner(world => Schedule(run));
            public static Runner Run(Action run) => Run((in TReact _) => run());

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static Runner RunEach(Filter filter, InAction<TReact, Segment> run, params IDependency[] dependencies) => new Runner(world =>
            {
                var components = world.Components();
                var segments = Array.Empty<Segment>();
                var index = 0;
                return Schedule((in TReact message) =>
                {
                    while (index < components.Segments.Length)
                    {
                        var segment = components.Segments[index++];
                        if (filter.Matches(segment)) ArrayUtility.Add(ref segments, components.Segments[index++]);
                    }

                    for (int i = 0; i < segments.Length; i++) run(message, segments[i]);
                }, dependencies.Prepend(new Read(typeof(Entity))));
            });

            static Result<Phase[]> Schedule(InAction<TReact> run, params IDependency[] dependencies) =>
                new[] { Phase.From(run, dependencies.Prepend(new Dependencies.React(typeof(TReact)))) };
        }

        public static partial class When<TReact, TReceive> where TReact : struct, IMessage where TReceive : struct, IMessage
        {
            public static Runner Run(InAction<TReact, TReceive> run) => new Runner(world =>
            {
                var messages = world.Messages();
                var receiver = messages.Receiver<TReceive>();
                return Schedule((in TReact react) => { while (receiver.TryMessage(out var receive)) run(react, receive); });
            });
            public static Runner Run(InAction<TReceive> run) => Run((in TReact react, in TReceive receive) => run(receive));
            public static Runner Run(Action run) => Run((in TReact react, in TReceive receive) => run());

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static Runner RunEach(Filter filter, InAction<TReact, TReceive, Segment> run, params IDependency[] dependencies) => new Runner(world =>
            {
                var components = world.Components();
                var messages = world.Messages();
                var receiver = messages.Receiver<TReceive>();
                var segments = Array.Empty<Segment>();
                var index = 0;
                return Schedule((in TReact react) =>
                {
                    while (index < components.Segments.Length)
                    {
                        var segment = components.Segments[index++];
                        if (filter.Matches(segment)) ArrayUtility.Add(ref segments, components.Segments[index++]);
                    }

                    while (receiver.TryMessage(out var receive))
                        for (int i = 0; i < segments.Length; i++) run(react, receive, segments[i]);
                }, dependencies.Prepend(new Read(typeof(Entity))));
            });

            static Result<Phase[]> Schedule(InAction<TReact> run, params IDependency[] dependencies) =>
                new[] { Phase.From(run, dependencies.Prepend(new Dependencies.React(typeof(TReact)), new Read(typeof(TReceive)))) };
        }

        public static Runner Lazy(Func<Runner> provide) => new Runner(world => provide().Schedule(world));

        public static Runner All(params Runner[] runners) => new Runner(world => runners
            .Select(runner => runner.Schedule(world))
            .All()
            .Map(phases => phases
                .Flatten()
                .GroupBy(phase => phase.Type)
                .Select(group => Phase.Combine(group.ToArray()))
                .Choose()
                .ToArray()));

        public readonly Schedule Schedule;
        public Runner(Schedule schedule) { Schedule = schedule; }
    }
}