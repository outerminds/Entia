using System;
using System.Threading.Tasks;

namespace Entia.Core
{
    public static class TaskExtensions
    {
        public static Task Timeout(this Task task, TimeSpan timeout) => Task.Run(async () =>
        {
            if (task == await Task.WhenAny(task, Task.Delay(timeout))) await task;
            else throw new TimeoutException();
        });

        public static Task Except<T>(this Task task, Action<T> handle) where T : Exception => Task.Run(async () =>
        {
            try { await task; }
            catch (T exception) { handle(exception); }
        });

        public static Task Except(this Task task, Action<Exception> handle) => Task.Run(async () =>
        {
            try { await task; }
            catch (Exception exception) { handle(exception); }
        });

        public static Task Do(this Task task, Action @do) => Task.Run(async () =>
        {
            await task;
            @do();
        });
    }
}