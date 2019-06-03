using Entia.Builders;
using Entia.Core;
using Entia.Modules.Build;
using Entia.Nodes;
using Entia.Phases;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Modules
{
    public sealed class Controllers : IModule, IClearable, IEnumerable<Controller>
    {
        readonly World _world;
        readonly Dictionary<Node, Controller> _controllers = new Dictionary<Node, Controller>();

        public Controllers(World world) { _world = world; }

        public Result<Controller> Control(Node node)
        {
            if (TryGet(node, out var controller)) return controller;

            var count = 0;
            var nodes = new Dictionary<Node, IRunner>();
            var runners = new Dictionary<IRunner, int>();
            var states = Array.Empty<Controller.States>();
            var root = node
                .Descend(child =>
                {
                    if (child.Value is IWrapper) return child;
                    var index = count++;
                    return child
                        .Wrap(new Map(runner => { runners[runner] = index; return runner; }))
                        .Wrap(new State(() => states[index]))
                        .Wrap(new Map(runner => { runners[runner] = index; return runner; }));
                })
                .Resolve()
                .Wrap(new Root())
                .Descend(child => child.Wrap(new Map(runner => nodes[child] = runner)));
            states = new Controller.States[count];

            return Result
                .And(_world.Analyzers().Analyze(root, root), _world.Builders().Build(root, root))
                .Map(pair => _controllers[node] = new Controller((root, pair.right), _world, nodes, runners, states));
        }

        public bool TryGet(Node node, out Controller controller) => _controllers.TryGetValue(node, out controller);
        public bool Has(Node node) => _controllers.ContainsKey(node);
        public bool Has(Controller controller) => _controllers.ContainsValue(controller);
        public bool Remove(Node node) => _controllers.Remove(node);
        public bool Clear() => _controllers.TryClear();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<Controller> GetEnumerator() => _controllers.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
