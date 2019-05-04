using Entia.Analyzers;
using Entia.Builders;
using Entia.Core;
using Entia.Dependencies;
using Entia.Modules;
using Entia.Modules.Analysis;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Phases;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Nodes
{
    public readonly struct Parallel : IAtomic, IAnalyzable<Parallel.Analyzer>, IBuildable<Parallel.Builder>
    {
        sealed class Runner : IRunner
        {
            public object Instance => Children;
            public readonly IRunner[] Children;
            public Runner(params IRunner[] children) { Children = children; }

            public IEnumerable<Type> Phases() => Children.SelectMany(child => child.Phases());
            public IEnumerable<Phase> Phases(Controller controller) => Children.SelectMany(child => child.Phases(controller));
            public Option<Run<T>> Specialize<T>(Controller controller) where T : struct, IPhase
            {
                var children = (items: new Run<T>[Children.Length], count: 0);
                foreach (var child in Children) if (child.Specialize<T>(controller).TryValue(out var special)) children.Push(special);

                switch (children.count)
                {
                    case 0: return Option.None();
                    case 1: return children.items[0];
                    default:
                        var runs = children.ToArray();
                        void Run(in T phase)
                        {
                            var local = phase;
                            global::System.Threading.Tasks.Parallel.For(0, runs.Length, index => runs[index](local));
                        }
                        return new Run<T>(Run);
                }
            }
        }

        sealed class Builder : IBuilder
        {
            public Result<IRunner> Build(Node node, Node root, World world) => node.Children.Length == 1 ?
                Result.Cast<Parallel>(node.Value).Bind(_ => world.Builders().Build(node.Children[0], root)) :
                Result.Cast<Parallel>(node.Value)
                    .Bind(_ => node.Children.Select(child => world.Builders().Build(child, root)).All())
                    .Map(children => new Runner(children))
                    .Cast<IRunner>();
        }

        sealed class Analyzer : Analyzer<Nodes.Parallel>
        {
            Result<Unit> Unknown(Node node, IDependency[] dependencies) =>
                dependencies.OfType<Unknown>().Select(_ => Result.Failure($"'{node}' has unknown dependencies.")).All();

            Result<Unit> WriteWrite((Node node, IDependency[] dependencies) left, (Node node, IDependency[] dependencies) right)
            {
                var writes = right.dependencies.Writes().ToArray();
                return left.dependencies.Writes()
                    .Where(type => writes.Any(write => type.Is(write, true, true)))
                    .Select(type => Result.Failure($"'{left.node}' and '{right.node}' both have a write dependency on type '{type.FullFormat()}'."))
                    .All();
            }

            Result<Unit> WriteRead((Node node, IDependency[] dependencies) left, (Node node, IDependency[] dependencies) right)
            {
                var reads = right.dependencies.Reads().ToArray();
                return left.dependencies.Writes()
                    .Where(type => reads.Any(read => type.Is(read, true, true)))
                    .Select(type => Result.Failure($"'{left.node}' has a write dependency on type '{type.FullFormat()}' and '{right.node}' reads from it."))
                    .All();
            }

            public override Result<IDependency[]> Analyze(Nodes.Parallel data, Node node, Node root, World world) =>
                node.Children.Select(child => world.Analyzers().Analyze(child, root).Map(dependencies => (child, dependencies))).All().Bind(children =>
                {
                    var combinations = children.Combinations(2).ToArray();
                    var unknown = children.Select(pair => Unknown(pair.child, pair.dependencies)).All();
                    var writeWrite1 = combinations.Select(pairs => WriteWrite(pairs[0], pairs[1])).All();
                    var writeWrite2 = combinations.Select(pairs => WriteWrite(pairs[1], pairs[0])).All();
                    var writeRead = combinations.Select(pairs => WriteRead(pairs[0], pairs[1])).All();
                    var readWrite = combinations.Select(pairs => WriteRead(pairs[1], pairs[0])).All();
                    return Result.All(unknown, writeWrite1, writeWrite2, writeRead, readWrite)
                        .Map(_ => children.SelectMany(pair => pair.dependencies)
                        .ToArray());
                });

        }
    }
}
