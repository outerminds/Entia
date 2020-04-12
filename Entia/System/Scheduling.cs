using Entia.Core;
using Entia.Experimental.Schedulers;

namespace Entia.Experimental.Scheduling
{
    public readonly struct Context
    {
        public readonly Node Node;
        public readonly World World;

        public Context(World world) : this(default, world) { }
        Context(Node node, World world)
        {
            Node = node;
            World = world;
        }

        public Result<Runner[]> Schedule(Node node) => World.Container.Get<IScheduler>(node.Data.GetType())
            .Select(With(node), (scheduler, state) => scheduler.Schedule(state))
            .Any();

        public Result<Runner[]> Schedule(params Node[] nodes) => nodes.Select(Schedule).All().Map(runners => runners.Flatten());

        public Context With(Node node) => new Context(node, World);
    }

    public static class Extensions
    {
        public static Result<Disposable> Schedule(this World world, in Node node) => new Context(world).Schedule(node)
            .Bind(runners => runners
                .Select(runner => Runner.Reaction(runner, world).And(runner)
                    .AsResult($"Expected to find reaction for runner of type '{runner.Type.FullFormat()}'."))
                .All()
                .Map(pairs =>
                {
                    foreach (var (reaction, runner) in pairs) reaction.Add(runner.Run);
                    return new Disposable(() => { foreach (var (reaction, runner) in pairs) reaction.Remove(runner.Run); });
                }));
    }
}