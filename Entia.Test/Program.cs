using System;
using System.Diagnostics;

namespace Entia.Test
{
	static class Program
	{
		static void Main() => Test.Run();

		static void Measure(string name, System.Action test, int iterations)
		{
			test();
			test();
			test();

			GC.Collect();
			GC.WaitForFullGCComplete();
			GC.WaitForPendingFinalizers();
			GC.Collect();

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

			Console.WriteLine($"{name}   ->   Total: {TimeSpan.FromTicks(total)} | Average: {TimeSpan.FromTicks(total / iterations)} | Minimum: {TimeSpan.FromTicks(minimum)} | Maximum: {TimeSpan.FromTicks(maximum)}");
		}
	}
}
