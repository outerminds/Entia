using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Entia.Core
{
    public static class TaskExtensions
    {
        public static Task Timeout(this Task task, TimeSpan timeout, CancellationTokenSource cancel = null) => Task.Run(async () =>
        {
            var timer = Stopwatch.StartNew();
            while (task.Status == TaskStatus.Running)
            {
                if (timer.Elapsed >= timeout)
                {
                    cancel?.Cancel();
                    throw new TimeoutException();
                }
            }
            await task;
        });

        public static Task<T> Timeout<T>(this Task<T> task, TimeSpan timeout, CancellationTokenSource cancel = null) => Task.Run(async () =>
        {
            var timer = Stopwatch.StartNew();
            while (task.Status == TaskStatus.Running)
            {
                if (timer.Elapsed >= timeout)
                {
                    cancel?.Cancel();
                    throw new TimeoutException();
                }
            }
            return await task;
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
            try { await task; @do(); }
            catch { throw; }
        });
    }
}