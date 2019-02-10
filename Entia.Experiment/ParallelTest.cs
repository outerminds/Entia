using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Entia.Core;

namespace Entia.Experiment
{
    public static class ParallelTest
    {
        public static void Run()
        {
            var queue = new ConcurrentQueue<int>();
            var run = For(100, queue.Enqueue);
            run();
            run();
            run();
        }

        static Action For(int count, Action<int> body)
        {
            var actions = new Action[count];
            for (int i = 0; i < count; i++) actions[i] = () => body(i);
            return Invoke(actions);
        }

        static Action Invoke(params Action[] actions)
        {
            var state = 0;
            var runs = actions
                .Select(action => new WaitCallback(_ => { action(); Interlocked.Increment(ref state); }))
                .ToArray();

            void Run()
            {
                state = 0;
                for (int i = 0; i < runs.Length; i++) ThreadPool.QueueUserWorkItem(runs[i]);
                while (state < runs.Length) { }
            }
            return new Action(Run);
        }

        static Action<T> Invoke<T>(params Action<T>[] actions)
        {
            var state = (done: 0, phase: default(T));
            var runs = actions
                .Select(action => new WaitCallback(_ => { action(state.phase); Interlocked.Increment(ref state.done); }))
                .ToArray();

            void Run(T input)
            {
                state = (0, input);
                for (int i = 0; i < runs.Length; i++) ThreadPool.QueueUserWorkItem(runs[i]);
                while (state.done < runs.Length) { }
            }
            return new Action<T>(Run);
        }
    }
}