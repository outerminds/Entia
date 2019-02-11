using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Entia.Core;

namespace Entia.Experiment
{
    public class WorkStealingQueue<T>
    {
        T[] _items = new T[32];
        int _mask = 31;
        volatile int _head;
        volatile int _tail;
        readonly object _lock = new object();

        public bool IsEmpty => _head >= _tail;
        public int Count => _tail - _head;

        public void Push(T value)
        {
            int tail = _tail;
            if (tail < _head + _mask)
            {
                _items[tail & _mask] = value;
                _tail = tail + 1;
            }
            else
            {
                lock (_lock)
                {
                    int head = _head;
                    int count = _tail - _head;
                    if (count >= _mask)
                    {
                        T[] target = new T[_items.Length << 1];
                        for (int i = 0; i < _items.Length; i++) target[i] = _items[(i + head) & _mask];

                        _items = target;
                        _head = 0;
                        _tail = tail = count;
                        _mask = (_mask << 1) | 1;
                    }

                    _items[tail & _mask] = value;
                    _tail = tail + 1;
                }
            }
        }

        public bool TryPop(out T value)
        {
            var tail = _tail;
            if (_head >= tail)
            {
                value = default;
                return false;
            }

            tail -= 1;
            Interlocked.Exchange(ref _tail, tail);

            if (_head <= tail)
            {
                value = _items[tail & _mask];
                return true;
            }
            else
            {
                lock (_lock)
                {
                    if (_head <= tail)
                    {
                        value = _items[tail & _mask];
                        return true;
                    }
                    else
                    {
                        _tail = tail + 1;
                        value = default;
                        return false;
                    }
                }
            }
        }

        public bool TrySteal(out T value, int timeout)
        {
            var taken = false;
            try
            {
                taken = Monitor.TryEnter(_lock, timeout);
                if (taken)
                {
                    int head = _head;
                    Interlocked.Exchange(ref _head, head + 1);

                    if (head < _tail)
                    {
                        value = _items[head & _mask];
                        return true;
                    }
                    else
                    {
                        _head = head;
                        value = default;
                        return false;
                    }
                }
            }
            finally { if (taken) Monitor.Exit(_lock); }

            value = default;
            return false;
        }
    }

    static class Boba
    {
        static readonly int _processors = Environment.ProcessorCount;
        static readonly ConcurrentQueue<Action>[] _queues = new ConcurrentQueue<Action>[_processors];
        static readonly Thread[] _threads = new Thread[_processors];
        static readonly object _lock = new object();
        static int _index;
        static int _count;

        static Boba()
        {
            for (int i = 0; i < _processors; i++)
            {
                var index = i;
                var thread = new Thread(() => Update(index));
                _threads[i] = thread;
                _queues[i] = new ConcurrentQueue<Action>();
                thread.Start();
            }

            var process = Process.GetCurrentProcess();
            for (int i = 0; i < _threads.Length; i++)
            {
                var thread = process.Threads.Cast<ProcessThread>().FirstOrDefault(current => current.Id == _threads[i].ManagedThreadId);
                if (thread == null) continue;
                thread.ProcessorAffinity = new IntPtr(1 << i);
            }
        }

        static void Update(int index)
        {
            while (true)
            {
                var done = true;
                for (int i = 0; i < _queues.Length; i++)
                {
                    var next = (index + i) % _queues.Length;
                    var queue = _queues[next];
                    while (queue.TryDequeue(out var work))
                    {
                        done = false;
                        work();
                        Interlocked.Decrement(ref _count);
                    }
                }

                if (done) lock (_lock) Monitor.Wait(_lock);
            }
        }

        public static void Schedule(Action work)
        {
            var index = _index++ % _processors;
            Interlocked.Increment(ref _count);
            _queues[index].Enqueue(work);
            lock (_lock) Monitor.PulseAll(_lock);
            while (_count > 0) { }
        }
    }
    public static class ParallelTest
    {
        public static void Run()
        {
            var queue = new ConcurrentQueue<int>();
            var run = For(100, queue.Enqueue);
            while (true)
            {
                queue.Clear();
                run();
            }
        }

        static Action For(int count, Action<int> body)
        {
            var actions = new Action[count];
            for (int i = 0; i < count; i++)
            {
                var index = i;
                actions[index] = () => body(index);
            }
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