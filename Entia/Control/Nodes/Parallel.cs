using Entia.Analysis;
using Entia.Build;
using Entia.Builders;
using Entia.Core;
using Entia.Dependencies;
using Entia.Dependency;
using Entia.Modules.Schedule;
using Entia.Phases;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Nodes
{
    public readonly struct Parallel : IAtomic, IImplementation<Parallel.Builder>, IImplementation<Parallel.Analyzer>
    {
        sealed class Runner : IRunner
        {
            public object Instance => Children;
            public readonly IRunner[] Children;
            public Runner(params IRunner[] children) { Children = children; }

            public IEnumerable<Type> Phases() => Children.SelectMany(child => child.Phases());
            public IEnumerable<Phase> Schedule(Controller controller) => Children.SelectMany(child => child.Schedule(controller));
            public Option<Run<T>> Specialize<T>(Controller controller) where T : struct, IPhase
            {
                var children = Children.Select(controller, (child, state) => child.Specialize<T>(state)).Choose().ToArray();
                switch (children.Length)
                {
                    case 0: return Option.None();
                    case 1: return children[0];
                    default:
                        void Run(in T phase)
                        {
                            var local = phase;
                            global::System.Threading.Tasks.Parallel.For(0, children.Length, index => children[index](local));
                        }
                        return new Run<T>(Run);
                }
            }
        }

        sealed class Builder : Builder<Parallel>
        {
            public override Result<IRunner> Build(in Parallel data, in Build.Context context)
            {
                var children = context.Node.Children;
                if (children.Length == 1) return context.Build(children[0]);
                return children
                    .Select(context, (child, state) => state.Build(child))
                    .All()
                    .Map(runners => new Runner(runners))
                    .Cast<IRunner>();
            }
        }

        sealed class Analyzer : Analyzer<Parallel>
        {
            Result<Unit> Unknown(Node node, IDependency[] dependencies) =>
                dependencies.OfType<Unknown>().Select(_ => Result.Failure($"'{node}' has unknown dependencies.").AsResult()).All();

            Result<Unit> WriteWrite((Node node, IDependency[] dependencies) left, (Node node, IDependency[] dependencies) right)
            {
                var writes = right.dependencies.Writes().ToArray();
                return left.dependencies.Writes()
                    .Where(type => writes.Any(write => type.Is(write, true, true)))
                    .Select(type => Result.Failure($"'{left.node}' and '{right.node}' both have a write dependency on type '{type.FullFormat()}'.").AsResult())
                    .All();
            }

            Result<Unit> WriteRead((Node node, IDependency[] dependencies) left, (Node node, IDependency[] dependencies) right)
            {
                var reads = right.dependencies.Reads().ToArray();
                return left.dependencies.Writes()
                    .Where(type => reads.Any(read => type.Is(read, true, true)))
                    .Select(type => Result.Failure($"'{left.node}' has a write dependency on type '{type.FullFormat()}' and '{right.node}' reads from it.").AsResult())
                    .All();
            }

            public override Result<IDependency[]> Analyze(in Nodes.Parallel data, in Analysis.Context context) => context.Node.Children
                .Select(context, (child, state) => state.Analyze(child)
                    .Map(dependencies => (child, dependencies)))
                .All()
                .Bind(pairs =>
                {
                    var combinations = pairs.Combinations(2).ToArray();
                    var unknown = pairs.Select(pair => Unknown(pair.child, pair.dependencies)).All();
                    var writeWrite1 = combinations.Select(combination => WriteWrite(combination[0], combination[1])).All();
                    var writeWrite2 = combinations.Select(combination => WriteWrite(combination[1], combination[0])).All();
                    var writeRead = combinations.Select(combination => WriteRead(combination[0], combination[1])).All();
                    var readWrite = combinations.Select(combination => WriteRead(combination[1], combination[0])).All();
                    return Result.All(new[] { unknown, writeWrite1, writeWrite2, writeRead, readWrite })
                        .Map(_ => pairs.SelectMany(pair => pair.dependencies)
                        .ToArray());
                });
        }
    }
}
