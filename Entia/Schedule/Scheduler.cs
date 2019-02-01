using System;
using System.Collections.Generic;
using Entia.Modules.Control;
using Entia.Modules.Schedule;

namespace Entia.Schedulers
{
    public interface ISchedulable { }
    public interface ISchedulable<T> : ISchedulable where T : IScheduler, new() { }
    public interface IScheduler
    {
        IEnumerable<Phase> Schedule(object instance, Controller controller, World world);
    }

    public abstract class Scheduler<T> : IScheduler where T : ISchedulable
    {
        public abstract IEnumerable<Phase> Schedule(T instance, Controller controller, World world);
        IEnumerable<Phase> IScheduler.Schedule(object instance, Controller controller, World world) =>
            instance is T casted ? Schedule(casted, controller, world) : Array.Empty<Phase>();
    }

    public sealed class Default : IScheduler
    {
        public IEnumerable<Phase> Schedule(object instance, Controller controller, World world) => Array.Empty<Phase>();
    }
}
