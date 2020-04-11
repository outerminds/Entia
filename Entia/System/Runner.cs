using System;
using Entia.Core;
using Entia.Dependencies;
using Entia.Modules;

namespace Entia.Experimental.Scheduling
{
    public delegate void Run(IntPtr message);
    public delegate bool React(in Runner runner, World world);

    public readonly struct Runner
    {
        static class Cache<T> where T : struct, IMessage
        {
            public static readonly React React = (in Runner runner, World world) =>
            {
                if (runner.Type == typeof(InAction<T>))
                {
                    var run = new InAction<T>((in T _) => throw null);
                    UnsafeUtility.Copy(runner.Run, run);
                    world.Messages().React(run);
                    return true;
                }
                return false;
            };
        }

        static readonly TypeMap<Delegate, React> _reacts = new TypeMap<Delegate, React>();

        public static Runner From<T>(InAction<T> run, params IDependency[] dependencies) where T : struct, IMessage
        {
            // NOTE: weird hack to ensure that 'Emitter<T>/Receiver<T>/Reaction<T>' are all properly AOT compiled
            _reacts.Set<InAction<T>>(Cache<T>.React);
            var target = new Run(_ => throw null);
            UnsafeUtility.Copy(run, target);
            return new Runner(typeof(InAction<T>), target, dependencies);
        }

        public static Option<Runner> Combine(in Runner left, in Runner right)
        {
            if (left.Type == right.Type)
                return new Runner(left.Type, left.Run + right.Run, left.Dependencies.Append(right.Dependencies));
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

        public static Option<Runner> Combine(Func<Run[], Run> combine, params Runner[] runners)
        {
            if (runners.Length == 0) return Option.None();

            var first = runners[0];
            var runs = new Run[runners.Length];
            for (int i = 0; i < runners.Length; i++)
            {
                var runner = runners[i];
                if (first.Type == runner.Type) runs[i] = runner.Run;
                else return Option.None();
            }
            return new Runner(first.Type, combine(runs), runners.Select(runner => runner.Dependencies).Flatten());
        }

        public static bool TryReact(in Runner runner, World world) => _reacts.TryGet(runner.Type, out var react) && react(runner, world);

        public readonly Type Type;
        public readonly Run Run;
        public readonly IDependency[] Dependencies;

        Runner(Type type, Run run, params IDependency[] dependencies)
        {
            Type = type;
            Run = run;
            Dependencies = dependencies;
        }

        public Runner With(params IDependency[] dependencies) => new Runner(Type, Run, dependencies);
    }
}