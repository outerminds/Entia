using System;
using System.Linq;
using Entia.Core;
using Entia.Dependencies;
using Entia.Experimental.Schedulers;
using Entia.Experimental.Scheduling;

namespace Entia.Experimental.Nodes
{
    public interface INode { }
    public interface IWrapper : IBranch { }
    public interface ILeaf : INode { }
    public interface IBranch : INode { }
    public interface IAtomic : INode { }

    public readonly struct Lazy : ILeaf
    {
        [Implementation]
        static Scheduler<Lazy> _scheduler => Scheduler.From((in Lazy data, in Context context) =>
            data.Provide(context.World).Bind(context.Schedule));

        public readonly Func<World, Result<Node>> Provide;
        public Lazy(Func<World, Result<Node>> provide) { Provide = provide; }
    }

    public readonly struct Schedule : ILeaf, IAtomic
    {
        [Implementation]
        static Scheduler<Schedule> _scheduler => Scheduler.From((in Schedule data, in Context context) =>
            data.Provide(context.World).Map(runner => new[] { runner }));

        public readonly Func<World, Result<Runner>> Provide;
        public Schedule(Func<World, Result<Runner>> provide) { Provide = provide; }
    }

    public readonly struct Depend : IWrapper
    {
        [Implementation]
        static Scheduler<Depend> _scheduler => Scheduler.From((in Depend data, in Context context) =>
            context.Schedule(context.Node.Children).Map(data.Dependencies, (runners, state) =>
                runners.Select(runner => runner.With(runner.Dependencies.Prepend(state)))));

        public readonly IDependency[] Dependencies;
        public Depend(params IDependency[] dependencies) { Dependencies = dependencies; }
    }

    public readonly struct Resolve : IWrapper
    {
        [Implementation]
        static Scheduler<Resolve> _scheduler => Scheduler.From((in Resolve data, in Context context) =>
            context.Schedule(context.Node.Children).Map(context.World, (runners, world) =>
                runners.Select(runner => Runner.Wrap(runner, () => world.Resolve(), () => world.Resolve())).Choose().ToArray()));
    }

    public readonly struct If : IWrapper
    {
        [Implementation]
        static Scheduler<If> _scheduler => Scheduler.From((in If data, in Context context) =>
            context.Schedule(context.Node.Children).Map(data.Condition, (runners, condition) =>
                runners.Select(runner => Runner.If(runner, condition)).Choose().ToArray()));

        public readonly Func<bool> Condition;
        public If(Func<bool> condition) { Condition = condition; }
    }

    public readonly struct Map : IWrapper
    {
        [Implementation]
        static Scheduler<Map> _scheduler => Scheduler.From((in Map data, in Context context) =>
            context.Schedule(context.Node.Children).Map(data.Mapper, (runners, mapper) =>
                runners.Select(runner => mapper(runner)).Choose().ToArray()));

        public readonly Func<Runner, Option<Runner>> Mapper;
        public Map(Func<Runner, Option<Runner>> mapper) { Mapper = mapper; }
    }

    public readonly struct Sequence : IBranch
    {
        [Implementation]
        static Scheduler<Sequence> _scheduler => Scheduler.From((in Sequence _, in Context context) =>
            context.Schedule(context.Node.Children).Map(runners => runners
                .GroupBy(runner => runner.Type)
                .Select(group => Runner.Combine(group.ToArray()))
                .Choose()
                .ToArray()));
    }

    public readonly struct Parallel : IBranch, IAtomic
    {
        [Implementation]
        static Scheduler<Parallel> _scheduler => Scheduler.From((in Parallel _, in Context context) =>
            context.Schedule(context.Node.Children).Map(runners => runners
                .GroupBy(runner => runner.Type)
                .Select(group => Runner.Combine(
                    (start, count, body) => System.Threading.Tasks.Parallel.For(start, count, body),
                    group.ToArray()))
                .Choose()
                .ToArray()));
    }
}