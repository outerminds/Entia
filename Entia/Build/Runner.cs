using Entia.Core;
using Entia.Modules.Schedule;
using Entia.Phases;
using System;
using System.Collections.Generic;

namespace Entia.Build
{
    public delegate void Run<T>(in T phase) where T : struct, IPhase;

    public interface IRunner
    {
        object Instance { get; }
        IEnumerable<Type> Phases();
        IEnumerable<Phase> Phases(Controller controller);
        Option<Run<T>> Specialize<T>(Controller controller) where T : struct, IPhase;
    }
}
