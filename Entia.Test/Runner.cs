using FsCheck;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;

namespace Entia.Test
{
    public sealed class MasterRunner
    {
        struct State
        {
            public int Iteration;
            public int Count;
            public int Size;
            public int Largest;
            public int Shrink;
            public TestResult Result;
        }

        readonly object _lock = new object();
        readonly State[] _states;
        readonly Stopwatch _watch = Stopwatch.StartNew();

        public MasterRunner(int count) { _states = new State[count]; }

        public void OnArguments(SlaveRunner slave, int value1, FSharpList<object> value2, FSharpFunc<int, FSharpFunc<FSharpList<object>, string>> value3)
        {
            ref var state = ref _states[slave.Index];
            var size = (value2[0] as ISized)?.Size ?? (value2[0] as IList)?.Count ?? -1;
            state.Iteration = value1;
            state.Count = slave.Count;
            state.Size = size;
            state.Largest = Math.Max(state.Largest, size);

            Log();
            FsCheck.Runner.consoleRunner.OnArguments(value1, value2, value3);
        }

        public void OnFinished(SlaveRunner slave, string value1, TestResult value2)
        {
            ref var state = ref _states[slave.Index];
            state.Result = value2;

            if (_states.All(current => current.Result != null))
            {
                var smallest = _states.OrderBy(current => current.Size).FirstOrDefault();
                switch (smallest.Result)
                {
                    case TestResult.False @false:
                        Console.WriteLine();
                        var original = @false.Item2.Select(value => value is IFormatted formatted ? Format.Wrap(formatted, Format.Types.Summary) : value);
                        var shrunk = @false.Item3.Select(value => value is IFormatted formatted ? Format.Wrap(formatted, Format.Types.Detailed) : value);
                        var wrapped = TestResult.NewFalse(@false.Item1, ListModule.OfSeq(original), ListModule.OfSeq(shrunk), @false.Item4, @false.Item5);
                        if (slave.Throw) throw new Exception();
                        FsCheck.Runner.consoleRunner.OnFinished(value1, wrapped);
                        Console.WriteLine();
                        break;
                }

                Console.WriteLine();
                Console.WriteLine($"Done in {_watch.Elapsed}");
            }
        }

        public void OnShrink(SlaveRunner slave, FSharpList<object> value1, FSharpFunc<FSharpList<object>, string> value2)
        {
            ref var state = ref _states[slave.Index];
            var size = (value1[0] as ISized)?.Size ?? (value1[0] as IList)?.Count ?? -1;
            state.Size = size;
            state.Shrink++;

            Log();
            FsCheck.Runner.consoleRunner.OnShrink(value1, value2);
        }

        public void OnStartFixture(SlaveRunner slave, Type value) => FsCheck.Runner.consoleRunner.OnStartFixture(value);

        void Log()
        {
            var lines = _states
                .Select(state => $"-> {{ Iterations: {state.Iteration + 1} / {state.Count}, \tSize: {state.Size}, \tLargest: {state.Largest}, \tShrink: {state.Shrink} }}")
                .Prepend("Running...")
                .Select(line => line + Environment.NewLine);
            var log = string.Join("", lines);

            lock (_lock)
            {
                Console.CursorVisible = false;
                Console.SetCursorPosition(0, 0);
                Console.Write(log);
                Console.CursorVisible = true;
            }
        }
    }

    public sealed class SlaveRunner : IRunner
    {
        public int Index { get; }
        public int Count { get; }
        public bool Throw { get; }

        readonly MasterRunner _master;

        public SlaveRunner(int index, int count, bool @throw, MasterRunner master)
        {
            Index = index;
            Count = count;
            Throw = @throw;
            _master = master;
        }

        public void OnArguments(int value1, FSharpList<object> value2, FSharpFunc<int, FSharpFunc<FSharpList<object>, string>> value3) => _master.OnArguments(this, value1, value2, value3);
        public void OnFinished(string value1, TestResult value2) => _master.OnFinished(this, value1, value2);
        public void OnShrink(FSharpList<object> value1, FSharpFunc<FSharpList<object>, string> value2) => _master.OnShrink(this, value1, value2);
        public void OnStartFixture(Type value) => _master.OnStartFixture(this, value);
    }

    public class Runner : IRunner
    {
        readonly int _count;
        readonly bool _throw;
        readonly Stopwatch _watch = Stopwatch.StartNew();
        int _largest;
        int _shrinks;

        public Runner(int count, bool @throw)
        {
            _count = count;
            _throw = @throw;
        }

        public void OnArguments(int value1, FSharpList<object> value2, FSharpFunc<int, FSharpFunc<FSharpList<object>, string>> value3)
        {
            var size = (value2[0] as ISized)?.Size ?? (value2[0] as IList)?.Count ?? -1;
            _largest = Math.Max(_largest, size);

            Console.CursorVisible = false;
            Console.CursorLeft = 0;
            Console.Write($"Running... {{ Iterations: {value1 + 1} / {_count}, \tSize: {size}, \tLargest: {_largest} }}");

            FsCheck.Runner.consoleRunner.OnArguments(value1, value2, value3);
        }

        public void OnFinished(string value1, TestResult value2)
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.CursorVisible = true;

            switch (value2)
            {
                case TestResult.False @false:
                    var original = @false.Item2.Select(value => value is IFormatted formatted ? Format.Wrap(formatted, Format.Types.Summary) : value);
                    var shrunk = @false.Item3.Select(value => value is IFormatted formatted ? Format.Wrap(formatted, Format.Types.Detailed) : value);
                    var wrapped = TestResult.NewFalse(@false.Item1, ListModule.OfSeq(original), ListModule.OfSeq(shrunk), @false.Item4, @false.Item5);
                    FsCheck.Runner.consoleRunner.OnFinished(value1, wrapped);
                    if (_throw) throw new Exception();
                    break;
                default:
                    FsCheck.Runner.consoleRunner.OnFinished(value1, value2);
                    break;
            }

            Console.WriteLine();
            Console.WriteLine($"Done in {_watch.Elapsed}");
            Console.WriteLine();
        }

        public void OnShrink(FSharpList<object> value1, FSharpFunc<FSharpList<object>, string> value2)
        {
            if (_shrinks == 0) Console.WriteLine();

            var size = (value1[0] as ISized)?.Size ?? (value1[0] as IList)?.Count ?? -1;
            Console.CursorVisible = false;
            Console.CursorLeft = 0;
            Console.Write($"Shrinking... {{ Iterations: {++_shrinks}, \tSize: {size}}}");
            FsCheck.Runner.consoleRunner.OnShrink(value1, value2);
        }

        public void OnStartFixture(Type value) => FsCheck.Runner.consoleRunner.OnStartFixture(value);
    }
}
