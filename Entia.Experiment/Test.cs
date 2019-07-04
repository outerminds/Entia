using System;
using System.Diagnostics;
using System.Linq;

namespace Entia.Experiment
{
    public static class Test
    {
        public static void Collect()
        {
            GC.Collect();
            GC.WaitForFullGCComplete();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public static void Measure(Action @base, Action[] tests, int iterations, int warmup = 10, Action before = null, Action after = null)
        {
            var watch = new Stopwatch();

            (string name, Action test, long total, long minimum, long maximum) Run(Action test)
            {
                for (int j = 0; j < warmup; j++) test();

                before?.Invoke();
                var total = 0L;
                var minimum = long.MaxValue;
                var maximum = long.MinValue;
                for (var i = 0; i < iterations; i++)
                {
                    watch.Restart();
                    test();
                    watch.Stop();
                    total += watch.ElapsedTicks;
                    minimum = Math.Min(minimum, watch.ElapsedTicks);
                    maximum = Math.Max(maximum, watch.ElapsedTicks);
                }
                after?.Invoke();

                return (test.Method.Name.Split("__").LastOrDefault().Split('|').FirstOrDefault(), test, total, minimum, maximum);
            }

            string Justify(object value, int length)
            {
                var format = value.ToString();
                return format + new string(' ', Math.Max(length - format.Length, 0));
            }

            string Format((string name, Action test, long total, long minimum, long maximum) result, double baseTotal)
            {
                var column = 20;
                var name = Justify(result.name, column);
                var total = Justify(TimeSpan.FromTicks(result.total), column);
                var ratio = Justify((result.total / baseTotal).ToString("0.000"), column);
                var average = Justify(TimeSpan.FromTicks(result.total / iterations), column);
                var minimum = Justify(TimeSpan.FromTicks(result.minimum), column);
                var maximum = Justify(TimeSpan.FromTicks(result.maximum), column);
                return $"{name} ->   Total: {total} Ratio: {ratio} Average: {average} Minimum: {minimum} Maximum: {maximum}";
            }

            var runners = tests.Prepend(@base).ToArray();
            runners.Shuffle();
            var results = new (string name, Action test, long total, long minimum, long maximum)[runners.Length];

            Collect();
            for (int i = 0; i < runners.Length; i++) results[i] = Run(runners[i]);

            Array.Sort(results, (a, b) => a.name.CompareTo(b.name));
            var reference = results.FirstOrDefault(result => result.test == @base).total;
            foreach (var result in results) Console.WriteLine(Format(result, reference));
        }

        public static void Measure(string name, Action test, int iterations)
        {
            test();
            test();
            test();

            long total = 0;
            var minimum = long.MaxValue;
            var maximum = long.MinValue;
            var watch = new Stopwatch();
            for (var i = 0; i < iterations; i++)
            {
                watch.Restart();
                test();
                watch.Stop();
                total += watch.ElapsedTicks;
                minimum = Math.Min(minimum, watch.ElapsedTicks);
                maximum = Math.Max(maximum, watch.ElapsedTicks);
            }

            Console.WriteLine($"{name} \t->   Total: {TimeSpan.FromTicks(total)} | Average: {TimeSpan.FromTicks(total / iterations)} | Minimum: {TimeSpan.FromTicks(minimum)} | Maximum: {TimeSpan.FromTicks(maximum)}");
        }
    }
}