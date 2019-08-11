using Entia.Core;
using Entia.Schedulables;
using Entia.Schedulers;
using Entia.Injectables;

namespace Entia.Systems
{
    public interface ISystem : ISchedulable, IInjectable, IImplementation<Injectors.System>, IImplementation<Dependers.Fields> { }
    public interface IInitialize : ISystem, IImplementation<Initialize> { void Initialize(); }
    public interface IRun : ISystem, IImplementation<Run> { void Run(); }
    public interface IDispose : ISystem, IImplementation<Dispose> { void Dispose(); }
    public interface IReact<T> : ISystem, IImplementation<Dependers.React<T>>, IImplementation<Schedulers.React<T>> where T : struct, IMessage
    {
        void React(in T message);
    }
}