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
    public readonly struct Phase : IEquatable<Phase>
    {
        public static Phase From<T>(InAction<T> action) where T : struct, IPhase => new Phase(action, typeof(T), PhaseUtility.Cache<T>.Index);
        public static Phase From<T>(Action action) where T : struct, IPhase => From((in T _) => action());

        public readonly Delegate Delegate;
        public readonly Type Type;
        public readonly int Index;

        Phase(Delegate @delegate, Type type, int index)
        {
            Delegate = @delegate;
            Type = type;
            Index = index;
        }

        public bool Equals(Phase other) => (Delegate, Type, Index) == (other.Delegate, other.Type, other.Index);
        public override bool Equals(object obj) => obj is Phase phase && Equals(phase);
        public override int GetHashCode() => (Delegate, Type, Index).GetHashCode();
    }
}
