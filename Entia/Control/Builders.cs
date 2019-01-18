using Entia.Core;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Nodes;
using Entia.Phases;
using Entia.Systems;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Entia.Builders
{
    public sealed class System : IBuilder
    {
        public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase
        {
            if (Option.Cast<Nodes.System>(node.Value).TryValue(out var data))
            {
                if (controller.Runners(node).Select(runner => runner.Instance).OfType(data.Type).FirstOrDefault() is ISystem system ||
                    world.Injectors().Inject<ISystem>(data.Type).TryValue(out system))
                {
                    var run = default(InAction<T>);
                    var phases = world.Schedulers().Schedule(system, controller);
                    foreach (var phase in phases) if (phase.Delegate is InAction<T> action) run += action;
                    // NOTE: return a runner even if 'run' is null.
                    return new Runner<T>(system, run);
                }
            }

            return Option.None();
        }
    }

    public sealed class Sequence : IBuilder
    {
        public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase
        {
            var children = new List<Runner<T>>(node.Children.Length);
            foreach (var child in node.Children)
            {
                if (world.Builders().Build<T>(child, controller).TryValue(out var current))
                    children.Add(current);
            }

            var runners = children.ToArray();
            switch (runners.Length)
            {
                case 0: return Option.None();
                case 1: return runners[0];
                default: return new Runner<T>(runners, (in T phase) => { foreach (var runner in runners) runner.Run(phase); });
            }
        }
    }

    public sealed class Parallel : IBuilder
    {
        public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase
        {
            var children = new List<Runner<T>>(node.Children.Length);
            foreach (var child in node.Children)
            {
                if (world.Builders().Build<T>(child, controller).TryValue(out var current))
                    children.Add(current);
            }

            var runners = children.ToArray();
            switch (runners.Length)
            {
                case 0: return Option.None();
                case 1: return runners[0];
                default:
                    return new Runner<T>(runners, (in T phase) =>
                    {
                        var local = phase;
                        global::System.Threading.Tasks.Parallel.For(0, runners.Length, index => runners[index].Run(local));
                    });
            }
        }
    }

    public sealed class Profile : IBuilder
    {
        public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase
        {
            if (world.Builders().Build<T>(Node.Sequence(node.Name, node.Children), controller).TryValue(out var runner))
            {
                var watch = new Stopwatch();
                var messages = world.Messages();
                return new Runner<T>(runner, (in T phase) =>
                {
                    watch.Restart();
                    runner.Run(phase);
                    watch.Stop();
                    messages.Emit(new OnProfile { Node = node, Phase = typeof(T), Elapsed = watch.Elapsed });
                });
            }

            return Option.None();
        }
    }

    public sealed class State : IBuilder
    {
        public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase
        {
            if (Option.Cast<Nodes.State>(node.Value).TryValue(out var data) &&
                world.Builders().Build<T>(Node.Sequence(node.Name, node.Children), controller).TryValue(out var runner))
                return new Runner<T>(runner, (in T phase) => { if (data.Get() == Controller.States.Enabled) runner.Run(phase); });

            return Option.None();
        }
    }

    public sealed class Map : IBuilder
    {
        public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase
        {
            if (Option.Cast<Nodes.Map>(node.Value).TryValue(out var data) &&
                world.Builders().Build<T>(Node.Sequence(node.Name, node.Children), controller).TryValue(out var runner))
                return data.Mapper(runner).Cast<Runner<T>>();

            return Option.None();
        }
    }

    public sealed class Resolve : IBuilder
    {
        public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase
        {
            if (typeof(T).Is<IResolve>() && Option.Cast<Nodes.Resolve>(node.Value).TryValue(out var data))
                return new Runner<T>(data, (in T _) => world.Resolve());

            return Option.None();
        }
    }

    public sealed class Automatic : IBuilder
    {
        public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase
        {
            switch (node.Children.Length)
            {
                case 0: return Option.None();
                case 1: return world.Builders().Build<T>(node.Children[0], controller);
            }

            var sets = new[] { node.Children, node.Children.Reverse() }
                .SelectMany(runners => runners
                    .Select(runner => runners
                        .Except(runner)
                        .Aggregate(
                            new[] { runner },
                            (group, current) =>
                            {
                                var nodes = group.Append(current).ToArray();
                                var parallel = Node.Parallel(nodes);
                                var result = world.Analyzers().Analyze(parallel, controller.Node);
                                return result.IsSuccess() ? nodes : group;
                            })
                        .ToSet()))
                .ToArray();

            var groups = new List<Node>();
            var remaining = new HashSet<Node>(node.Children);
            while (remaining.Count > 0)
            {
                var set = sets
                    .Where(group => group.Count > 0)
                    .OrderByDescending(group => group.Count)
                    .Select(group => group.ToArray())
                    .FirstOrDefault();
                if (set == null) return Option.None();

                sets.Iterate(group => group.ExceptWith(set));
                remaining.ExceptWith(set);
                groups.Add(Node.Parallel(set));
            }

            return world.Builders().Build<T>(Node.Sequence(node.Name, groups.ToArray()), controller);
        }
    }
}
