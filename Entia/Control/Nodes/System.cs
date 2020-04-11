using Entia.Analysis;
using Entia.Build;
using Entia.Builders;
using Entia.Core;
using Entia.Dependencies;
using Entia.Dependency;
using Entia.Injection;
using Entia.Modules.Schedule;
using Entia.Phases;
using Entia.Schedulers;
using Entia.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Nodes
{
    public readonly struct System : IAtomic, IImplementation<System.Analyzer>, IImplementation<System.Builder>
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
            public IEnumerable<Phase> Schedule(Controller controller) =>
                _phases.TryGetValue(controller, out var phases) ? phases :
                // NOTE: do not filter phases here to allow parent nodes to receive all phases
                _phases[controller] = new Scheduling.Context(controller, controller.World).Schedule(System).ToArray();
            public Option<Run<T>> Specialize<T>(Controller controller) where T : struct, IPhase
            {
                var run = default(Run<T>);
                foreach (var phase in Schedule(controller)
                    .Where(phase => phase.Target == Phase.Targets.System)
                    .DistinctBy(phase => phase.Distinct))
                    run += phase.Delegate as Run<T>;
                return Option.From(run);
            }
        }

        sealed class Builder : Builder<System>
        {
            public override Result<IRunner> Build(in System data, in Build.Context context) =>
                context.World.Inject<ISystem>(data.Type)
                    .Map(context, (system, state) =>
                        new Runner(system, state.World.Container.Get<IScheduler>(system.GetType()).ToArray()))
                    .Cast<IRunner>();
        }

        sealed class Analyzer : Analyzer<System>
        {
            public override Result<IDependency[]> Analyze(in System data, in Analysis.Context context)
            {
                var world = context.World;
                var dependencies = world.Dependencies(data.Type);
                var emits = dependencies.Emits().ToArray();
                if (emits.Length > 0)
                {
                    var direct = context.Root.Family()
                        .Select(child => Option.Cast<Nodes.System>(child.Value).Map(system => (child, system)))
                        .Choose()
                        .Select(pair => (pair.child, dependencies: world.Dependencies(pair.system.Type)))
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
