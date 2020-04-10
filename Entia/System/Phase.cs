using System;
using Entia.Core;
using Entia.Dependencies;
using Entia.Modules;

namespace Entia.Experimental
{
    public delegate void Run(IntPtr message);
    public delegate bool React(in Phase phase, World world);

    public readonly struct Phase
    {
        static readonly TypeMap<Delegate, React> _reacts = new TypeMap<Delegate, React>();

        public static Phase From<T>(InAction<T> run, params IDependency[] dependencies) where T : struct, IMessage
        {
            EnsureReact<T>();
            var target = new Run(_ => throw null);
            UnsafeUtility.Copy(run, target);
            return new Phase(typeof(InAction<T>), target, dependencies);
        }

        public static Option<Phase> Combine(in Phase left, in Phase right)
        {
            if (left.Type == right.Type)
                return new Phase(left.Type, left.Run + right.Run, left.Dependencies.Append(right.Dependencies));
            return Option.None();
        }

        public static Option<Phase> Combine(params Phase[] phases)
        {
            if (phases.Length == 0) return Option.None();

            var phase = phases[0];
            for (int i = 1; i < phases.Length; i++)
            {
                if (Combine(phase, phases[i]).TryValue(out phase)) continue;
                return Option.None();
            }
            return phase;
        }

        public static Option<Phase> Combine(Func<Run[], Run> combine, params Phase[] phases)
        {
            if (phases.Length == 0) return Option.None();

            var first = phases[0];
            var runs = new Run[phases.Length];
            for (int i = 0; i < phases.Length; i++)
            {
                var phase = phases[i];
                if (first.Type == phase.Type) runs[i] = phase.Run;
                else return Option.None();
            }
            return new Phase(first.Type, combine(runs), phases.Select(phase => phase.Dependencies).Flatten());
        }

        public static bool TryReact(in Phase phase, World world) => _reacts.TryGet(phase.Type, out var react) && react(phase, world);

        // NOTE: weird hack to ensure that 'Emitter<T>/Receiver<T>/Reaction<T>' are all properly AOT compiled
        static bool EnsureReact<T>() where T : struct, IMessage
        {
            if (_reacts.Has<InAction<T>>()) return false;
            return _reacts.Set<InAction<T>>((in Phase phase, World world) =>
            {
                if (phase.Type == typeof(InAction<T>))
                {
                    var target = new InAction<T>((in T _) => throw null);
                    UnsafeUtility.Copy(phase.Run, target);
                    world.Messages().React(target);
                    return true;
                }
                return false;
            });
        }

        public readonly Type Type;
        public readonly Run Run;
        public readonly IDependency[] Dependencies;

        Phase(Type type, Run run, params IDependency[] dependencies)
        {
            Type = type;
            Run = run;
            Dependencies = dependencies;
        }

        public Phase With(params IDependency[] dependencies) => new Phase(Type, Run, dependencies);
    }
}