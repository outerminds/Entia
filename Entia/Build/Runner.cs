using Entia.Core;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Phases;
using System;
using System.Collections.Generic;

namespace Entia.Modules.Build
{
    public interface IRunner
    {
        object Instance { get; }
        IEnumerable<Type> Phases();
        IEnumerable<Phase> Phases(Controller controller);
        Option<Runner<T>> Specialize<T>(Controller controller) where T : struct, IPhase;
    }

    public readonly struct Runner<T> where T : struct, IPhase
    {
        public static readonly Runner<T> Empty = new Runner<T>((in T _) => { });

        public readonly InAction<T> Run;
        public Runner(InAction<T> run) { Run = run; }
    }
}
