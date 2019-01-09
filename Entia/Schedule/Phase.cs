using Entia.Core;
using Entia.Phases;
using System;

namespace Entia.Phases
{
    public interface IPhase { }
}

namespace Entia.Modules.Schedule
{
    public readonly struct Phase
    {
        public readonly Delegate Delegate;
        public readonly Type Type;
        public readonly int Index;

        Phase(Delegate @delegate, Type type, int index)
        {
            Delegate = @delegate;
            Type = type;
            Index = index;
        }

        public static Phase From<T>(InAction<T> action) where T : struct, IPhase => new Phase(action, typeof(T), PhaseUtility.Cache<T>.Index);
        public static Phase From<T>(Action action) where T : struct, IPhase => From((in T _) => action());
    }
}
