using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Entia.Core
{
    public static class TaskExtensions
    {
        public static Task Timeout(this Task task, TimeSpan timeout) => Task.Run(async () =>
        {
            var timer = Stopwatch.StartNew();
            while (task.Status == TaskStatus.Running) if (timer.Elapsed >= timeout) throw new TimeoutException();
            await task;
        });

        public static Task<T> Timeout<T>(this Task<T> task, TimeSpan timeout) => Task.Run(async () =>
        {
            var timer = Stopwatch.StartNew();
            while (task.Status == TaskStatus.Running) if (timer.Elapsed >= timeout) throw new TimeoutException();
            return await task;
        });
    }
}