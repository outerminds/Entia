using Entia.Core;
using Entia.Core.Documentation;
using Entia.Modules.Build;
using Entia.Nodes;
using Entia.Phases;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules.Control
{
    public sealed class Controller
    {
        public enum States : byte { Enabled, Disabled }

        public readonly (Node node, IRunner runner) Root;
        public readonly World World;

        public IEnumerable<Node> Nodes => _nodeToRunner.Keys;
        public IEnumerable<IRunner> Runners => _runnerToIndex.Keys;

        readonly TypeMap<IPhase, object> _phaseToRunner = new TypeMap<IPhase, object>();
        readonly Dictionary<Node, IRunner> _nodeToRunner;
        readonly Dictionary<IRunner, int> _runnerToIndex;
        readonly States[] _states;

        public Controller((Node node, IRunner runner) root, World world, Dictionary<Node, IRunner> nodes, Dictionary<IRunner, int> runners, States[] states)
        {
            Root = root;
            World = world;
            _nodeToRunner = nodes;
            _runnerToIndex = runners;
            _states = states;
        }

        public bool TryIndex(IRunner runner, out int index) => _runnerToIndex.TryGetValue(runner, out index);

        public bool TryIndex(Node node, out int index)
        {
            if (TryRunner(node, out var runner)) return TryIndex(runner, out index);
            index = default;
            return false;
        }

        public bool TryRunner(Node node, out IRunner runner)
        {
            if (node == Root.node)
            {
                runner = Root.runner;
                return true;
            }
            return _nodeToRunner.TryGetValue(node, out runner);
        }

        public bool TryRunner<T>(out Runner<T> runner) where T : struct, IPhase
        {
            if (_phaseToRunner.TryGet<T>(out var box) && box is Box<Runner<T>> casted)
            {
                runner = casted.Value;
                return true;
            }

            runner = default;
            return false;
        }
        public bool Has<T>() where T : struct, IPhase => _phaseToRunner.Has<T>();
        public bool Has(Type phase) => _phaseToRunner.Has(phase);

        public bool TryState(Node node, out States state) => TryIndex(node, out var index) & TryState(index, out state);
        public bool TryState(IRunner runner, out States state) => TryIndex(runner, out var index) & TryState(index, out state);
        public bool TryState(int index, out States state)
        {
            if (index < _states.Length)
            {
                state = _states[index];
                return true;
            }

            state = default;
            return false;
        }

        public bool Enable() => Enable(Root.runner);
        public bool Enable(Node node) => TryIndex(node, out var index) && Enable(index);
        public bool Enable(IRunner runner) => TryIndex(runner, out var index) && Enable(index);
        public bool Enable(int index) => _states[index].Change(States.Enabled);

        public bool Disable() => Disable(Root.runner);
        public bool Disable(Node node) => TryIndex(node, out var index) && Disable(index);
        public bool Disable(IRunner runner) => TryIndex(runner, out var index) && Disable(index);
        public bool Disable(int index) => _states[index].Change(States.Disabled);

        public void Run<T>() where T : struct, IPhase => Runner<T>().Run(default);
        public void Run<T>(in T phase) where T : struct, IPhase => Runner<T>().Run(phase);

        public Runner<T> Runner<T>() where T : struct, IPhase
        {
            if (_phaseToRunner.TryGet<T>(out var runner) && runner is Runner<T> casted) return casted;
            casted = Root.runner.Specialize<T>(this).Or(Build.Runner<T>.Empty);
            _phaseToRunner.Set<T>(casted);
            return casted;
        }
    }
}
