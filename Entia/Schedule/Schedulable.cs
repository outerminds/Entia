using Entia.Schedulers;

namespace Entia.Schedulables
{
    public interface ISchedulable { }
    public interface ISchedulable<T> : ISchedulable where T : IScheduler, new() { }
}
