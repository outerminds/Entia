namespace Entia.Phases
{
    public interface IResolve : IPhase { }

    public readonly struct Initialize : IResolve { }
    public readonly struct Run : IResolve { }
    public readonly struct Dispose : IResolve { }
    public struct React<T> : IPhase where T : struct, IMessage { public T Message; }

    public static class React
    {
        public readonly struct Initialize : IResolve { }
        public readonly struct Dispose : IResolve { }
    }
}
