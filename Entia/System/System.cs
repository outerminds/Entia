using System;
using Entia.Core;
using Entia.Phases;

namespace Entia.Experimental
{
    public delegate void Run<TPhase>(in TPhase phase);

    public readonly struct Runner
    {
        public readonly TypeMap<IPhase, Delegate> Runs;
        public Runner(params (Type phase, Delegate Run)[] runs) : this(new TypeMap<IPhase, Delegate>(runs)) { }
        public Runner(TypeMap<IPhase, Delegate> runs) { Runs = runs; }
    }

    public readonly struct Runner<TRun> where TRun : Delegate
    {
        public readonly TRun Run;
        public Runner(TRun run) { Run = run; }

        public Runner<TPhase, TRun> Schedule<TPhase>(Func<TRun, Run<TPhase>> adapt) where TPhase : struct, IPhase =>
            new Runner<TPhase, TRun>(Run, adapt);
    }

    public readonly struct Runner<TPhase, TRun> where TPhase : struct, IPhase where TRun : Delegate
    {
        public static implicit operator Runner(in Runner<TPhase, TRun> system) =>
            new Runner((typeof(TPhase), system.Adapt(system.Run)));

        public readonly TRun Run;
        public readonly Func<TRun, Run<TPhase>> Adapt;
        public Runner(TRun run, Func<TRun, Run<TPhase>> adapt) => (Run, Adapt) = (run, adapt);
    }
}