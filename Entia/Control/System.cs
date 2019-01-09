using Entia.Dependables;
using Entia.Injectables;
using Entia.Schedulers;

namespace Entia.Systems
{
	[Members]
	public interface ISystem : ISchedulable, IInjectable<Injectors.System> { }
	public interface IPreInitialize : ISystem, ISchedulable<PreInitialize> { void PreInitialize(); }
	public interface IPostInitialize : ISystem, ISchedulable<PostInitialize> { void PostInitialize(); }
	public interface IInitialize : ISystem, ISchedulable<Initialize> { void Initialize(); }
	public interface IPreRun : ISystem, ISchedulable<PreRun> { void PreRun(); }
	public interface IPostRun : ISystem, ISchedulable<PostRun> { void PostRun(); }
	public interface IRun : ISystem, ISchedulable<Run> { void Run(); }
	public interface IPreDispose : ISystem, ISchedulable<PreDispose> { void PreDispose(); }
	public interface IPostDispose : ISystem, ISchedulable<PostDispose> { void PostDispose(); }
	public interface IDispose : ISystem, ISchedulable<Dispose> { void Dispose(); }
	public interface IPreReact<T> : ISystem, IDepend<Reaction<T>>, ISchedulable<Schedulers.PreReact<T>> where T : struct, IMessage
	{
		void PreReact(in T message);
	}
	public interface IReact<T> : ISystem, IDepend<Reaction<T>>, ISchedulable<Schedulers.React<T>> where T : struct, IMessage
	{
		void React(in T message);
	}
	public interface IPostReact<T> : ISystem, IDepend<Reaction<T>>, ISchedulable<Schedulers.PostReact<T>> where T : struct, IMessage
	{
		void PostReact(in T message);
	}
}