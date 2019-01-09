using Entia.Phases;
using System;
using System.Collections.Generic;

namespace Entia.Modules.Schedule
{
	public static class PhaseUtility
	{
		public static class Cache<T> where T : struct, IPhase
		{
			public static readonly int Index = GetIndex(typeof(T));
		}

		static readonly Dictionary<Type, int> _indices = new Dictionary<Type, int>();

		public static int GetIndex(Type phase) => TryGetIndex(phase, out var index) ? index : _indices[phase] = _indices.Count;
		public static bool TryGetIndex(Type phase, out int index) => _indices.TryGetValue(phase, out index);
	}
}