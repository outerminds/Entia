using Entia.Analyzers;
using Entia.Builders;
using Entia.Core;
using Entia.Dependencies;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Phases;
using Entia.Schedulers;
using Entia.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Nodes
{
    public readonly struct System : IAtomic
    {
        sealed class Runner : IRunner
        {
            public object Instance => System;
            public readonly ISystem System;
            public readonly IScheduler[] Schedulers;
            public Runner(ISystem system, params IScheduler[] schedulers)
            {
                System = system;
                Schedulers = schedulers;
            }

            public IEnumerable<Type> Phases() => Schedulers.SelectMany(scheduler => scheduler.Phases);
            public IEnumerable<Phase> Phases(Controller controller) => Schedulers.SelectMany(scheduler => scheduler.Schedule(System, controller));
            public Option<Runner<T>> Specialize<T>(Controller controller) where T : struct, IPhase
            {
                var run = default(InAction<T>);
                var set = new HashSet<object>();
                foreach (var phase in Phases(controller))
                {
                    if (phase.Target == Phase.Targets.System && phase.Type == typeof(T) && set.Add(phase.Distinct) && phase.Delegate is InAction<T> @delegate)
                        run += @delegate;
                }
                if (run == null) return Option.None();
                return new Runner<T>(run);
            }
        }

        sealed class Builder : Builder<Runner>
        {
            public override Result<Runner> Build(Node node, Node root, World world) => Result.Cast<System>(node.Value)
                .Bind(data => world.Injectors().Inject<ISystem>(data.Type))
                .Map(system =>
                {
                    var schedulers = system.GetType().Hierarchy()
                        .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ISchedulable<>))
                        .Select(type => world.Schedulers().Get(type))
                        .ToArray();
                    return new Runner(system, schedulers);
                });
        }

        sealed class Analyzer : Analyzer<System>
        {
            static IDependency[] Dependencies(Type type, World world) => world.Dependers().Dependencies(type);

            public override Result<IDependency[]> Analyze(System data, Node node, Node root, World world)
            {
                var dependencies = Dependencies(data.Type, world);
                var emits = dependencies.Emits().ToArray();
                if (emits.Length > 0)
                {
                    var direct = root.Family()
                        .Select(child => Option.Cast<Nodes.System>(child.Value).Map(system => (child, system)))
                        .Choose()
                        .Select(pair => (pair.child, dependencies: Dependencies(pair.system.Type, world)))
                        .ToArray();
                    dependencies = direct
                        .Where(pair => pair.dependencies.Reacts().Intersect(emits).Any())
                        .SelectMany(pair => pair.dependencies)
                        .Prepend(dependencies)
                        .Distinct()
                        .ToArray();
                }

                return dependencies;
            }
        }

        [Analyzer]
        static readonly Analyzer _analyzer = new Analyzer();
        [Builder]
        static readonly Builder _builder = new Builder();

        public readonly Type Type;

        public System(Type type) { Type = type; }
    }
}
