using System;
using Entia.Core;
using Entia.Modules.Schedule;
using Entia.Scheduling;

namespace Entia.Schedulers
{
    public interface IScheduler : ITrait
    {
        Type[] Phases { get; }
        Phase[] Schedule(in Context context);
    }

    public abstract class Scheduler<T> : IScheduler
    {
        public abstract Type[] Phases { get; }
        public abstract Phase[] Schedule(T instance, in Context context);
        Phase[] IScheduler.Schedule(in Context context) =>
            context.Instance is T casted ? Schedule(casted, context) : Array.Empty<Phase>();
    }
}
