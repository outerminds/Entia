using Entia.Core;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Nodes;
using Entia.Phases;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Modules
{
    public sealed class Controllers : IModule, IEnumerable<Controller>
    {
        readonly World _world;
        readonly Dictionary<Node, Controller> _controllers = new Dictionary<Node, Controller>();

        public Controllers(World world) { _world = world; }

        public Result<Controller> Control(Node node)
        {
            if (_controllers.TryGetValue(node, out var controller)) return controller;

            var count = 0;
            var nodes = new Dictionary<Node, int>();
            var runners = new Dictionary<Node, TypeMap<IPhase, IRunner>>();
            var states = default(Controller.States[]);
            var descend = node
                .Descend(current =>
                {
                    var index = count++;
                    var wrapped = current
                        .Map(runner =>
                        {
                            runners.GetOrAdd(current, () => new TypeMap<IPhase, IRunner>()).Set(runner.Phase, runner);
                            if (runner.Run == null) return Option.None();
                            return Option.Some(runner);
                        })
                        .State(() => states[index]);
                    nodes[current] = index;
                    nodes[wrapped] = index;
                    return wrapped;
                });
            states = new Controller.States[count];

            var result = _world.Analyzers().Analyze(descend, descend);
            if (result.TryValue(out _)) return _controllers[node] = new Controller(descend, _world, nodes, runners, states);
            return result.AsFailure();
        }

        public IEnumerator<Controller> GetEnumerator() => _controllers.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
