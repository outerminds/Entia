using Entia.Analyzers;
using Entia.Builders;
using Entia.Core;
using Entia.Dependencies;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Phases;
using Entia.Systems;
using System;
using System.Linq;

namespace Entia.Nodes
{
    public readonly struct System : IAtomic
    {
        sealed class Builder : IBuilder
        {
            public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase
            {
                if (Option.Cast<Nodes.System>(node.Value).TryValue(out var data) &&
                    controller.Runners(node).Select(runner => runner.Instance).OfType(data.Type).FirstOrDefault() is ISystem system ||
                    world.Injectors().Inject<ISystem>(data.Type).TryValue(out system))
                {
                    var run = default(InAction<T>);
                    var phases = world.Schedulers().Schedule(system, controller);
                    foreach (var phase in phases) if (phase.Delegate is InAction<T> action) run += action;
                    // NOTE: return a runner even if 'run' is null.
                    return new Runner<T>(system, run);
                }

                return Option.None();
            }
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
