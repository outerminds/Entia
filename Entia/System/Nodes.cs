using System;
using System.Linq;
using Entia.Core;
using Entia.Dependencies;
using Entia.Experimental.Schedulers;
using Entia.Experimental.Scheduling;

namespace Entia.Experimental.Nodes
{
    public interface INode { }
    public interface IWrapper : INode { }
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

    public readonly struct Schedule : ILeaf
    {
        [Implementation]
        static Scheduler<Schedule> _scheduler => Scheduler.From((in Schedule data, in Context context) =>
            data.Provide(context.World).Map(runner => new[] { runner }));

        public readonly Func<World, Result<Runner>> Provide;
        public Schedule(Func<World, Result<Runner>> provide) { Provide = provide; }
    }

    public readonly struct Depend : IWrapper, IBranch
    {
        [Implementation]
        static Scheduler<Depend> _scheduler => Scheduler.From((in Depend data, in Context context) =>
            context.Schedule(context.Node.Children).Map(data.Dependencies, (runners, state) =>
                runners.Select(runner => runner.With(runner.Dependencies.Prepend(state)))));

        public readonly IDependency[] Dependencies;
        public Depend(params IDependency[] dependencies) { Dependencies = dependencies; }
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
                .Select(group => Runner.Combine(runs => message => global::System.Threading.Tasks.Parallel.ForEach(runs, run => run(message)), group.ToArray()))
                .Choose()
                .ToArray()));
    }
}