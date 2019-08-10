using Entia.Build;
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

        public static Phase From<T>(Run<T> action, Targets target = Targets.System, object distinct = null) where T : struct, IPhase =>
            new Phase(action, typeof(T), target, distinct);
        public static Phase From<T>(Action action, Targets target = Targets.System, object distinct = null) where T : struct, IPhase =>
            From((in T _) => action(), target, distinct);

        public readonly Delegate Delegate;
        public readonly Type Type;
        public readonly Targets Target;
        public readonly object Distinct;

        Phase(Delegate @delegate, Type type, Targets target = Targets.System, object distinct = null)
        {
            Delegate = @delegate;
            Type = type;
            Target = target;
            Distinct = distinct ?? new object();
        }
    }
}
