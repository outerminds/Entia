namespace Entia.Phases
{
	public readonly struct PreInitialize : IPhase { }
	public readonly struct PostInitialize : IPhase { }
	public readonly struct Initialize : IPhase { }
	public readonly struct PreRun : IPhase { }
	public readonly struct PostRun : IPhase { }
	public readonly struct Run : IPhase { }
	public readonly struct PreDispose : IPhase { }
	public readonly struct PostDispose : IPhase { }
	public readonly struct Dispose : IPhase { }
	public struct PreReact<T> : IPhase where T : struct, IMessage { public T Message; }
	public struct PostReact<T> : IPhase where T : struct, IMessage { public T Message; }
	public struct React<T> : IPhase where T : struct, IMessage { public T Message; }

	public static class React
	{
		public readonly struct Initialize : IPhase { }
		public readonly struct Dispose : IPhase { }
	}
}
