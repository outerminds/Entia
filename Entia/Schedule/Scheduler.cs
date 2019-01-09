using Entia.Modules.Control;
using Entia.Modules.Schedule;

namespace Entia.Schedulers
{
	public interface ISchedulable { }
	public interface ISchedulable<T> : ISchedulable where T : IScheduler, new() { }
	public interface IScheduler
	{
		Phase[] Schedule(object instance, Controller controller, World world);
	}

	public abstract class Scheduler<T> : IScheduler where T : ISchedulable
	{
		public abstract Phase[] Schedule(T instance, Controller controller, World world);
		Phase[] IScheduler.Schedule(object instance, Controller controller, World world) =>
			instance is T casted ? Schedule(casted, controller, world) : new Phase[0];
	}

	public sealed class Default : IScheduler
	{
		public Phase[] Schedule(object instance, Controller controller, World world) => new Phase[0];
	}
}
