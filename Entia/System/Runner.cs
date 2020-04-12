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
            public static readonly Func<Runner[], Loop, Delegate> Invoke = (runners, loop) =>
            {
                var runs = runners.Select(runner => runner.Run).OfType<InAction<T>>().ToArray();
                return new InAction<T>((in T message) =>
                {
                    var copy = message;
                    loop(0, runs.Length, index => runs[index](copy));
                });
            };
        }

        static readonly TypeMap<Delegate, Func<World, IReaction>> _reactions = new TypeMap<Delegate, Func<World, IReaction>>();
        static readonly TypeMap<Delegate, Func<Runner[], Loop, Delegate>> _invokes = new TypeMap<Delegate, Func<Runner[], Loop, Delegate>>();

        public static Runner From<T>(InAction<T> run, params IDependency[] dependencies) where T : struct, IMessage
        {
            // NOTE: weird hack to ensure that 'Emitter<T>/Receiver<T>/Reaction<T>' are all properly AOT compiled
            _reactions.Set<InAction<T>>(Cache<T>.Reaction);
            _invokes.Set<InAction<T>>(Cache<T>.Invoke);
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

            var runner = runners[0];
            if (_invokes.TryGet(runner.Type, out var invoke) && invoke(runners, loop) is var run)
                return new Runner(runner.Type, run, runners.Select(runner => runner.Dependencies).Flatten());
            else
                return Option.None();
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