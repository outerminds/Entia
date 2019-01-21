using Entia.Analyzers;
using Entia.Builders;
using Entia.Core;
using Entia.Dependencies;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Phases;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Nodes
{
    public readonly struct Parallel : IAtomic
    {
        sealed class Builder : IBuilder
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

        sealed class Analyzer : Analyzer<Nodes.Parallel>
        {
            Result<Unit> Unknown(Node node, IDependency[] dependencies) =>
                dependencies.OfType<Unknown>().Select(_ => Result.Failure($"'{node}' has unknown dependencies.")).All();

            Result<Unit> WriteWrite((Node node, IDependency[] dependencies) left, (Node node, IDependency[] dependencies) right) =>
                left.dependencies.Writes()
                    .Intersect(right.dependencies.Writes())
                    .Select(type => Result.Failure($"'{left.node}' and '{right.node}' both have a write dependency on type '{type.FullFormat()}'."))
                    .All();

            Result<Unit> WriteRead((Node node, IDependency[] dependencies) left, (Node node, IDependency[] dependencies) right) =>
                left.dependencies.Writes()
                    .Intersect(right.dependencies.Reads())
                    .Select(type => Result.Failure($"'{left.node}' has a write dependency on type '{type.FullFormat()}' and '{right.node}' reads from it."))
                    .All();

            public override Result<IDependency[]> Analyze(Nodes.Parallel data, Node node, Node root, World world) =>
                node.Children.Select(child => world.Analyzers().Analyze(child, root).Map(dependencies => (child, dependencies))).All().Bind(children =>
                {
                    var combinations = children.Combinations(2).ToArray();
                    var unknown = children.Select(pair => Unknown(pair.child, pair.dependencies)).All();
                    var writeWrite = combinations.Select(pairs => WriteWrite(pairs[0], pairs[1])).All();
                    var writeRead = combinations.Select(pairs => WriteRead(pairs[0], pairs[1])).All();
                    var readWrite = combinations.Select(pairs => WriteRead(pairs[1], pairs[0])).All();

                    return Result.All(unknown, writeWrite, writeRead, readWrite)
                        .Map(__ => children.SelectMany(pair => pair.dependencies)
                        .ToArray());
                });

        }

        [Analyzer]
        static readonly Analyzer _analyzer = new Analyzer();
        [Builder]
        static readonly Builder _builder = new Builder();
    }
}
