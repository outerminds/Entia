﻿using Entia.Builders;
using Entia.Core;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Nodes;
using Entia.Phases;
using System;
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
            var nodes = new Dictionary<Node, IRunner>();
            var runners = new Dictionary<IRunner, int>();
            var states = Array.Empty<Controller.States>();
            var root = node
                .Wrap(new Root())
                .Descend(current =>
                {
                    var index = count++;
                    var mapped = current.Wrap(new Map(runner =>
                    {
                        runners[runner] = index;
                        return nodes[current] = runner;
                    }));
                    var wrapped = mapped.Wrap(new State(() => states[index]));
                    return wrapped.Wrap(new Map(runner =>
                    {
                        runners[runner] = index;
                        return nodes[wrapped] = nodes[mapped] = runner;
                    }));
                });
            states = new Controller.States[count];

            return Result
                .And(_world.Analyzers().Analyze(root, root), _world.Builders().Build(root, root))
                .Map(pair => _controllers[node] = new Controller((root, pair.right), _world, nodes, runners, states));
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<Controller> GetEnumerator() => _controllers.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
