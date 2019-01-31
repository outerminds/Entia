using Entia.Dependables;
using Entia.Dependers;
using Entia.Injectables;
using Entia.Schedulers;

namespace Entia.Systems
{
    public interface ISystem : ISchedulable, IInjectable<Injectors.System>, IDependable<Dependers.Fields> { }
    public interface IInitialize : ISystem, ISchedulable<Initialize> { void Initialize(); }
    public interface IRun : ISystem, ISchedulable<Run> { void Run(); }
    public interface IDispose : ISystem, ISchedulable<Dispose> { void Dispose(); }
    public interface IReact<T> : ISystem, IDependable<Dependers.React<T>>, ISchedulable<Schedulers.React<T>> where T : struct, IMessage
    {
        void React(in T message);
    }
}