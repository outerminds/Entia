namespace Entia.Phases
{
    public interface IResolve : IPhase { }

    public readonly struct PreInitialize : IResolve { }
    public readonly struct PostInitialize : IResolve { }
    public readonly struct Initialize : IResolve { }
    public readonly struct PreRun : IResolve { }
    public readonly struct PostRun : IResolve { }
    public readonly struct Run : IResolve { }
    public readonly struct PreDispose : IResolve { }
    public readonly struct PostDispose : IResolve { }
    public readonly struct Dispose : IResolve { }
    public struct PreReact<T> : IPhase where T : struct, IMessage { public T Message; }
    public struct PostReact<T> : IPhase where T : struct, IMessage { public T Message; }
    public struct React<T> : IPhase where T : struct, IMessage { public T Message; }

    public static class React
    {
        public readonly struct Initialize : IResolve { }
        public readonly struct Dispose : IResolve { }
    }
}
