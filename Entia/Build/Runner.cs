using Entia.Core;
using Entia.Phases;
using System;

namespace Entia.Modules.Build
{
	public interface IRunner
	{
		Type Phase { get; }
		object Instance { get; }
		Delegate Run { get; }
	}

	public readonly struct Runner<T> : IRunner, IEquatable<Runner<T>> where T : struct, IPhase
	{
		public static readonly Runner<T> Empty = new Runner<T>(null, (in T _) => { });

		public readonly object Instance;
		public readonly InAction<T> Run;

		Type IRunner.Phase => typeof(T);
		object IRunner.Instance => Instance;
		Delegate IRunner.Run => Run;

		public Runner(object instance, InAction<T> run)
		{
			Instance = instance;
			Run = run;
		}

		public bool Equals(Runner<T> other) => Run == other.Run;
		public override bool Equals(object obj) => obj is Runner<T> runner && Equals(runner);
		public override int GetHashCode() => Instance?.GetHashCode() ?? 0 ^ Run?.GetHashCode() ?? 0;
	}
}
