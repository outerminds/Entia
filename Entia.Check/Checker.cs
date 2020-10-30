using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entia.Core;

namespace Entia.Check
{
    public sealed class Failure<T>
    {
        public readonly T Original;
        public readonly T Shrinked;
        public readonly Property<T> Property;
        public readonly int Iteration;
        public readonly int Seed;
        public readonly double Size;

        public Failure(T original, T shrinked, Property<T> property, int iteration, int seed, double size)
        {
            Original = original;
            Shrinked = shrinked;
            Property = property;
            Iteration = iteration;
            Seed = seed;
            Size = size;
        }
    }

    public sealed class Checker<T>
    {
        public readonly Generator<T> Generator;
        public readonly int Iterations = 1_000 * Environment.ProcessorCount;
        public readonly int Parallel = Environment.ProcessorCount;
        public readonly Property<T>[] Properties;
        public readonly Action OnPre = () => { };
        public readonly Action<Failure<T>[]> OnPost = _ => { };
        public readonly Action<double> OnProgress = _ => { };

        public Checker(Generator<T> generator, params Property<T>[] properties)
        {
            Generator = generator;
            Properties = properties;
        }

        Checker(Generator<T> generator, Property<T>[] properties, int iterations, int parallel, Action onPre, Action<Failure<T>[]> onPost, Action<double> onProgress)
        {
            Generator = generator;
            Iterations = iterations;
            Parallel = parallel;
            Properties = properties;
            OnPre = onPre;
            OnPost = onPost;
            OnProgress = onProgress;
        }

        public Checker<T> With(Generator<T> generator = null, Property<T>[] properties = null, int? iterations = null, int? parallel = null, Action onPre = null, Action<Failure<T>[]> onPost = null, Action<double> onProgress = null) =>
            new Checker<T>(generator ?? Generator, properties ?? Properties, iterations ?? Iterations, parallel ?? Parallel, onPre ?? OnPre, onPost ?? OnPost, onProgress ?? OnProgress);
    }

    public static class Checker
    {
        public static Checker<T> Prove<T>(this Generator<T> generator, Func<T, bool> prove) =>
            generator.Prove(prove.Method.Name, prove);
        public static Checker<T> Prove<T>(this Generator<T> generator, string name, Func<T, bool> prove) =>
            generator.Prove(Property.From(name, prove));
        public static Checker<T> Prove<T>(this Generator<T> generator, params Property<T>[] properties) =>
            new Checker<T>(generator, properties);
        public static Checker<T> Prove<T>(this Checker<T> checker, Func<T, bool> prove) =>
            checker.Prove(prove.Method.Name, prove);
        public static Checker<T> Prove<T>(this Checker<T> checker, string name, Func<T, bool> prove) =>
            checker.Prove(Property.From(name, prove));
        public static Checker<T> Prove<T>(this Checker<T> checker, params Property<T>[] properties) =>
            checker.With(properties: checker.Properties.Append(properties));

        public static Checker<T> Log<T>(this Checker<T> checker, string name = null)
        {
            name ??= checker.Generator.Method.Name;
            return checker.With(
               onPre: () =>
               {
                   Console.CursorVisible = false;
                   Console.WriteLine();
               },
               onProgress: progress =>
               {
                   Console.CursorLeft = 0;
                   Console.Write($"Checking '{name}' {checker.Iterations / checker.Parallel}x{checker.Parallel}... {progress * 100:0.00}%");
               },
               onPost: failures =>
               {
                   Console.WriteLine();
                   Console.CursorVisible = true;
                   if (failures.Length == 0) Console.WriteLine("Success");
                   else Console.WriteLine($"{string.Join("", failures.Select(failure => $"{Environment.NewLine}-> Property '{failure.Property.Name}' failed with value '{failure.Shrinked}'"))}");
               });
        }

        public static Failure<T>[] Check<T>(this Checker<T> checker)
        {
            var iterations = checker.Iterations / checker.Parallel;
            var progress = new double[checker.Parallel];
            var task = Task.WhenAll(Enumerable.Range(0, checker.Parallel).Select(index => Task.Run(() => Run(index))));

            checker.OnPre();
            var last = 0.0;
            checker.OnProgress(progress.Average());
            while (!task.IsCompleted) if (last.Change(progress.Average())) checker.OnProgress(last);
            checker.OnProgress(progress.Average());
            var results = task.Result.Choose().ToArray();
            checker.OnPost(results);
            return results;

            Option<Failure<T>> Run(int index)
            {
                var iterations = checker.Iterations / checker.Parallel;
                var random = new Random();
                for (var i = 0; i <= iterations; i++)
                {
                    var seed = random.Next() ^ Thread.CurrentThread.ManagedThreadId ^ i ^ index ^ Environment.TickCount;
                    var size = progress[index] = i / (double)iterations;
                    var state = new Generator.State(size, 0, new Random(seed));
                    var (value, shrinked) = checker.Generator(state);
                    foreach (var property in checker.Properties)
                    {
                        if (property.Prove(value)) continue;
                        return new Failure<T>(value, Shrink(), property, i, seed, size);

                        T Shrink()
                        {
                            var @continue = true;
                            while (@continue.Change(false))
                            {
                                foreach (var generator in shrinked)
                                {
                                    var state = new Generator.State(size, 0, new Random(seed));
                                    var pair = generator(state);
                                    if (property.Prove(pair.value)) continue;
                                    (value, shrinked) = pair;
                                    @continue = true;
                                    break;
                                }
                            }
                            return value;
                        }
                    }
                }
                return Option.None();
            }

        }
    }
}