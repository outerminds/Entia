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
        Type[] Phases { get; }
        Phase[] Schedule(object instance, Controller controller);
    }

    public abstract class Scheduler<T> : IScheduler where T : ISchedulable
    {
        public abstract Type[] Phases { get; }
        public abstract Phase[] Schedule(T instance, Controller controller);
        Phase[] IScheduler.Schedule(object instance, Controller controller) =>
            instance is T casted ? Schedule(casted, controller) : Array.Empty<Phase>();
    }

    public sealed class Default : IScheduler
    {
        public Type[] Phases => Array.Empty<Type>();
        public Phase[] Schedule(object instance, Controller controller) => Array.Empty<Phase>();
    }
}
