using Entia.Core;
using Entia.Experimental.Nodes;
using Entia.Experimental.Scheduling;

namespace Entia.Experimental.Schedulers
{
    public interface IScheduler : ITrait
    {
        Result<Runner[]> Schedule(in Context context);
    }

    public abstract class Scheduler<T> : IScheduler where T : struct, INode
    {
        public abstract Result<Runner[]> Schedule(in T data, in Context context);
        Result<Runner[]> IScheduler.Schedule(in Context context) => Result.Cast<T>(context.Node.Value).Bind(context, (data, state) => Schedule(data, state));
    }

    public static class Scheduler
    {
        public static Scheduler<T> From<T>(InFunc<T, Result<Runner[]>> schedule) where T : struct, INode => From((in T data, in Context _) => schedule(data));
        public static Scheduler<T> From<T>(InFunc<T, Context, Result<Runner[]>> schedule) where T : struct, INode => new Function<T>(schedule);
    }

    public sealed class Function<T> : Scheduler<T> where T : struct, INode
    {
        readonly InFunc<T, Context, Result<Runner[]>> _schedule;
        public Function(InFunc<T, Context, Result<Runner[]>> schedule) { _schedule = schedule; }
        public override Result<Runner[]> Schedule(in T data, in Context context) => _schedule(data, context);
    }
}