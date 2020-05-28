using System;
using System.Linq;
using Entia.Core;
using Entia.Dependencies;
using Entia.Modules;
using Entia.Modules.Message;

namespace Entia.Experimental.Scheduling
{
    public readonly struct Runner
    {
        public delegate void Loop(int start, int count, Action<int> body);

        static class Cache<T> where T : struct, IMessage
        {
            public static readonly Func<World, IReaction> Reaction = world => world.Messages().Reaction<T>();
            public static readonly Func<Runner[], Loop, Runner> Loop = (runners, loop) =>
            {
                var runs = runners.Select(runner => runner.Run).Cast<InAction<T>>();
                return From((in T message) =>
                {
                    var copy = message;
                    loop(0, runs.Length, index => runs[index](copy));
                }, runners.Select(runner => runner.Dependencies).Flatten());
            };
            public static readonly Func<Runner, Action, Action, Runner> Wrap = (runner, before, after) =>
            {
                before ??= () => { };
                after ??= () => { };
                var run = (InAction<T>)runner.Run;
                return From((in T message) => { before(); run(message); after(); }, runner.Dependencies);
            };
            public static readonly Func<Runner, Func<bool>, Runner> If = (runner, condition) =>
            {
                condition ??= () => true;
                var run = (InAction<T>)runner.Run;
                return From((in T message) => { if (condition()) run(message); }, runner.Dependencies);
            };
        }

        static readonly TypeMap<Delegate, Func<World, IReaction>> _reactions = new TypeMap<Delegate, Func<World, IReaction>>();
        static readonly TypeMap<Delegate, Func<Runner[], Loop, Runner>> _loops = new TypeMap<Delegate, Func<Runner[], Loop, Runner>>();
        static readonly TypeMap<Delegate, Func<Runner, Action, Action, Runner>> _wraps = new TypeMap<Delegate, Func<Runner, Action, Action, Runner>>();
        static readonly TypeMap<Delegate, Func<Runner, Func<bool>, Runner>> _ifs = new TypeMap<Delegate, Func<Runner, Func<bool>, Runner>>();

        public static Runner From<T>(InAction<T> run, params IDependency[] dependencies) where T : struct, IMessage
        {
            // NOTE: weird hack to ensure that 'Emitter<T>/Receiver<T>/Reaction<T>' are all properly AOT compiled
            _reactions.Set<InAction<T>>(Cache<T>.Reaction);
            _loops.Set<InAction<T>>(Cache<T>.Loop);
            _wraps.Set<InAction<T>>(Cache<T>.Wrap);
            _ifs.Set<InAction<T>>(Cache<T>.If);
            return new Runner(typeof(InAction<T>), run, dependencies);
        }

        public static Option<Runner> Combine(in Runner left, in Runner right)
        {
            if (left.Type == right.Type)
                return new Runner(left.Type, Delegate.Combine(left.Run, right.Run), left.Dependencies.Append(right.Dependencies));
            return Option.None();
        }

        public static Option<Runner> Combine(params Runner[] runners)
        {
            if (runners.Length == 0) return Option.None();

            var runner = runners[0];
            for (int i = 1; i < runners.Length; i++)
            {
                if (Combine(runner, runners[i]).TryValue(out runner)) continue;
                return Option.None();
            }
            return runner;
        }

        public static Option<Runner> Combine(Loop loop, params Runner[] runners)
        {
            if (runners.Length < 2) return Combine(runners);
            if (_loops.TryGet(runners[0].Type, out var invoke)) return invoke(runners, loop);
            else return Option.None();
        }

        public static Option<Runner> Wrap(in Runner runner, Action before = null, Action after = null)
        {
            if (_wraps.TryGet(runner.Type, out var wrap)) return wrap(runner, before, after);
            else return Option.None();
        }

        public static Option<Runner> If(in Runner runner, Func<bool> condition)
        {
            if (_ifs.TryGet(runner.Type, out var @if)) return @if(runner, condition);
            else return Option.None();
        }

        public static Option<IReaction> Reaction(in Runner runner, World world) =>
            _reactions.TryGet(runner.Type, out var get) ? Option.From(get(world)) : Option.None();

        public readonly Type Type;
        public readonly Delegate Run;
        public readonly IDependency[] Dependencies;

        Runner(Type type, Delegate run, params IDependency[] dependencies)
        {
            Type = type;
            Run = run;
            Dependencies = dependencies;
        }

        public Runner With(params IDependency[] dependencies) => new Runner(Type, Run, dependencies);
    }
}