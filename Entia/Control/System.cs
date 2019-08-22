using Entia.Core;
using Entia.Injectables;
using Entia.Messages;

namespace Entia.Systems
{
    public interface ISystem : IInjectable, IImplementation<Injectors.System>, IImplementation<Dependers.Fields> { }
    public interface IInitialize : ISystem, IImplementation<Schedulers.Initialize> { void Initialize(); }
    public interface IRun : ISystem, IImplementation<Schedulers.Run> { void Run(); }
    public interface IDispose : ISystem, IImplementation<Schedulers.Dispose> { void Dispose(); }
    public interface IReact<T> : ISystem, IImplementation<Dependers.React<T>>, IImplementation<Schedulers.OnMessage<T>> where T : struct, IMessage
    {
        void React(in T message);
    }
    public interface IOnAdd<T> : ISystem, IImplementation<Dependers.React<OnAdd<T>, T>>, IImplementation<Schedulers.OnAdd<T>> where T : struct, IComponent
    {
        void OnAdd(Entity entity, ref T component);
    }
    public interface IOnRemove<T> : ISystem, IImplementation<Dependers.React<OnRemove<T>, T>>, IImplementation<Schedulers.OnRemove<T>> where T : struct, IComponent
    {
        void OnRemove(Entity entity, ref T component);
    }
    public interface IOnEnable<T> : ISystem, IImplementation<Dependers.React<OnEnable<T>, T>>, IImplementation<Schedulers.OnEnable<T>> where T : struct, IComponent
    {
        void OnEnable(Entity entity, ref T component);
    }
    public interface IOnDisable<T> : ISystem, IImplementation<Dependers.React<OnDisable<T>, T>>, IImplementation<Schedulers.OnDisable<T>> where T : struct, IComponent
    {
        void OnDisable(Entity entity, ref T component);
    }
}