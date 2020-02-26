using Entia.Build;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Nodes;
using Entia.Phases;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Entia
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
        readonly ConcurrentDictionary<Type, Delegate> _phaseToRun = new ConcurrentDictionary<Type, Delegate>();

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
        public bool Has<T>() where T : struct, IPhase => Has(typeof(T));
        [ThreadSafe]
        public bool Has(Type phase) => _phaseToRun.ContainsKey(phase);

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
            if (_phaseToRun.TryGetValue(typeof(T), out var run) && run is Run<T> casted) return casted;
            return CreateRun<T>();
        }

        Run<T> CreateRun<T>() where T : struct, IPhase =>
            (Run<T>)_phaseToRun.GetOrAdd(typeof(T), _ => Root.runner.Specialize<T>(this).Or(Cache<T>.Empty));
    }
}
