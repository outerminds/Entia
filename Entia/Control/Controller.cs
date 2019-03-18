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

        [ThreadSafe]
        static class Cache<T> where T : struct, IPhase
        {
            public static Run<T> Empty = (in T _) => { };
        }

        public readonly (Node node, IRunner runner) Root;
        public readonly World World;
        public readonly Node[] Nodes;
        public readonly IRunner[] Runners;

        readonly Dictionary<Node, IRunner> _nodeToRunner;
        readonly Dictionary<IRunner, int> _runnerToIndex;
        readonly States[] _states;
        readonly Concurrent<TypeMap<IPhase, Delegate>> _phaseToRun = new TypeMap<IPhase, Delegate>();

        public Controller((Node node, IRunner runner) root, World world, Dictionary<Node, IRunner> nodes, Dictionary<IRunner, int> runners, States[] states)
        {
            Root = root;
            World = world;
            Nodes = nodes.Keys.ToArray();
            Runners = runners.Keys.ToArray();
            _nodeToRunner = nodes;
            _runnerToIndex = runners;
            _states = states;
        }

        [ThreadSafe]
        public bool TryIndex(IRunner runner, out int index) => _runnerToIndex.TryGetValue(runner, out index);

        [ThreadSafe]
        public bool TryIndex(Node node, out int index)
        {
            if (TryRunner(node, out var runner)) return TryIndex(runner, out index);
            index = default;
            return false;
        }

        [ThreadSafe]
        public bool TryRunner(Node node, out IRunner runner)
        {
            if (node == Root.node)
            {
                runner = Root.runner;
                return true;
            }
            return _nodeToRunner.TryGetValue(node, out runner);
        }

        [ThreadSafe]
        public bool Has<T>() where T : struct, IPhase => _phaseToRun.Read(map => map.Has<T>(false, false));
        [ThreadSafe]
        public bool Has(Type phase) => _phaseToRun.Read(phase, (map, state) => map.Has(state, false, false));

        [ThreadSafe]
        public bool TryState(Node node, out States state) => TryIndex(node, out var index) & TryState(index, out state);
        [ThreadSafe]
        public bool TryState(IRunner runner, out States state) => TryIndex(runner, out var index) & TryState(index, out state);
        [ThreadSafe]
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

        [ThreadSafe]
        public void Run<T>() where T : struct, IPhase => Run<T>(DefaultUtility.Default<T>());
        [ThreadSafe]
        public void Run<T>(in T phase) where T : struct, IPhase => GetRun<T>()(phase);

        [ThreadSafe]
        Run<T> GetRun<T>() where T : struct, IPhase
        {
            using (var read = _phaseToRun.Read(true))
            {
                if (read.Value.TryGet<T>(out var run, false, false) && run is Run<T> casted1) return casted1;
                else
                {
                    casted1 = Root.runner.Specialize<T>(this).Or(Cache<T>.Empty);
                    using (var write = _phaseToRun.Write())
                    {
                        if (write.Value.TryGet<T>(out run, false, false) && run is Run<T> casted2) return casted2;
                        write.Value.Set<T>(casted1);
                        return casted1;
                    }
                }
            }
        }
    }
}
