using Entia.Core;
using Entia.Core.Documentation;
using Entia.Phases;
using System;

namespace Entia.Phases
{
    /// <summary>
    /// Tag interface that all phases must implement.
    /// </summary>
    public interface IPhase { }
}

namespace Entia.Modules.Schedule
{
    [ThreadSafe]
    public readonly struct Phase
    {
        public enum Targets { System, Root }

        public static Phase From<T>(InAction<T> action, Targets target = Targets.System, object distinct = null) where T : struct, IPhase =>
            new Phase(action, typeof(T), PhaseUtility.Cache<T>.Index, target, distinct);
        public static Phase From<T>(Action action, Targets target = Targets.System, object distinct = null) where T : struct, IPhase =>
            From((in T _) => action(), target, distinct);

        public readonly Delegate Delegate;
        public readonly Type Type;
        public readonly int Index;
        public readonly Targets Target;
        public readonly object Distinct;

        Phase(Delegate @delegate, Type type, int index, Targets target = Targets.System, object distinct = null)
        {
            Delegate = @delegate;
            Type = type;
            Index = index;
            Target = target;
            Distinct = distinct ?? new object();
        }
    }
}
