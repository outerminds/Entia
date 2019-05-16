using Entia.Analyzers;
using Entia.Builders;
using Entia.Core;
using Entia.Dependencies;
using Entia.Modules;
using Entia.Modules.Analysis;
using Entia.Modules.Build;
using Entia.Modules.Schedule;
using Entia.Phases;
using Entia.Schedulables;
using Entia.Schedulers;
using Entia.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Nodes
{
    public readonly struct System : IAtomic, IAnalyzable<System.Analyzer>, IBuildable<System.Builder>
    {
        sealed class Runner : IRunner
        {
            public object Instance => System;
            public readonly ISystem System;
            public readonly IScheduler[] Schedulers;

            readonly Dictionary<Controller, Phase[]> _phases = new Dictionary<Controller, Phase[]>();

            public Runner(ISystem system, params IScheduler[] schedulers)
            {
                System = system;
                Schedulers = schedulers;
            }

            public IEnumerable<Type> Phases() => Schedulers.SelectMany(scheduler => scheduler.Phases);
            public IEnumerable<Phase> Phases(Controller controller) =>
                _phases.TryGetValue(controller, out var phases) ? phases :
                _phases[controller] = Schedulers.SelectMany(scheduler => scheduler.Schedule(System, controller)).ToArray();
            public Option<Run<T>> Specialize<T>(Controller controller) where T : struct, IPhase
            {
                var run = default(Run<T>);
                var set = new HashSet<object>();
                foreach (var phase in Phases(controller))
                {
                    if (phase.Target == Phase.Targets.System && phase.Type == typeof(T) && set.Add(phase.Distinct) && phase.Delegate is Run<T> @delegate)
                        run += @delegate;
                }
                if (run == null) return Option.None();
                return run;
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
                        .Select(world.Schedulers().Get)
                        .ToArray();
                    return new Runner(system, schedulers);
                });
        }

        sealed class Analyzer : Analyzer<System>
        {
            public override Result<IDependency[]> Analyze(System data, Node node, Node root, World world)
            {
                var dependers = world.Dependers();
                var dependencies = dependers.Dependencies(data.Type);
                var emits = dependencies.Emits().ToArray();
                if (emits.Length > 0)
                {
                    var direct = root.Family()
                        .Select(child => Option.Cast<Nodes.System>(child.Value).Map(system => (child, system)))
                        .Choose()
                        .Select(pair => (pair.child, dependencies: dependers.Dependencies(pair.system.Type)))
                        .ToArray();
                    dependencies = direct
                        .Where(pair => pair.dependencies.Reacts().Any(react => emits.Any(emit => react.Is(emit, true, true))))
                        .SelectMany(pair => pair.dependencies)
                        .Prepend(dependencies)
                        .Distinct()
                        .ToArray();
                }

                return dependencies;
            }
        }

        public readonly Type Type;

        public System(Type type) { Type = type; }
    }
}
