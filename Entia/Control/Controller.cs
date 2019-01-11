using Entia.Core;
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

        public readonly Node Node;
        public readonly World World;

        readonly TypeMap<IPhase, IRunner> _phaseToRunner = new TypeMap<IPhase, IRunner>();
        readonly Dictionary<Node, int> _nodeToIndex;
        readonly Dictionary<Node, TypeMap<IPhase, IRunner>> _nodeToRunners = new Dictionary<Node, TypeMap<IPhase, IRunner>>();
        readonly States[] _states;

        public Controller(Node node, World world, Dictionary<Node, int> nodes, Dictionary<Node, TypeMap<IPhase, IRunner>> runners, States[] states)
        {
            Node = node;
            World = world;
            _nodeToIndex = nodes;
            _nodeToRunners = runners;
            _states = states;
        }

        public bool TryIndex(Node node, out int index) => _nodeToIndex.TryGetValue(node, out index);

        public IEnumerable<IRunner> Runners(Node node) => _nodeToRunners.TryGetValue(node, out var map) ? map.Values : Enumerable.Empty<IRunner>();
        public bool TryRunner(Node node, Type phase, out IRunner runner)
        {
            if (_nodeToRunners.TryGetValue(node, out var map) && map.TryGet(phase, out runner)) return true;
            runner = default;
            return false;
        }
        public bool TryRunner<T>(Node node, out Runner<T> runner) where T : struct, IPhase
        {
            if (_nodeToRunners.TryGetValue(node, out var map) && map.TryGet<T>(out var value) && value is Runner<T> casted)
            {
                runner = casted;
                return true;
            }
            runner = default;
            return false;
        }
        public bool TryRunner<T>(out Runner<T> runner) where T : struct, IPhase
        {
            if (_phaseToRunner.TryGet<T>(out var value) && value is Runner<T> casted)
            {
                runner = casted;
                return true;
            }

            runner = default;
            return false;
        }

        public bool TryState(Node node, out States state) => TryIndex(node, out var index) & TryState(index, out state);
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

        public bool Enable() => Enable(Node);
        public bool Enable(Node node) => TryIndex(node, out var index) && Enable(index);
        public bool Enable(int index) => _states[index].Change(States.Enabled);

        public bool Disable() => Disable(Node);
        public bool Disable(Node node) => TryIndex(node, out var index) && Disable(index);
        public bool Disable(int index) => _states[index].Change(States.Disabled);

        public void Run<T>(bool resolve = true) where T : struct, IPhase => Run(default(T), resolve);

        public void Run<T>(in T phase, bool resolve = true) where T : struct, IPhase
        {
            var runner = Runner<T>();
            runner.Run(phase);
            if (resolve) World.Resolve();
        }

        public Runner<T> Runner<T>() where T : struct, IPhase
        {
            if (TryRunner<T>(out var runner)) return runner;
            runner = World.Builders().Build<T>(Node, this).Or(Build.Runner<T>.Empty);
            _phaseToRunner.Set<T>(runner);
            return runner;
        }
    }
}
