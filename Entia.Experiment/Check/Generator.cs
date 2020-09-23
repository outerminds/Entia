using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Entia.Core;

namespace Entia.Experiment.Check
{
    public delegate (T value, IEnumerable<Generator<T>> shrinked) Generator<T>(Generator.State state);

    public static class Generator
    {
        public sealed class State
        {
            public readonly double Size;
            public readonly Random Random;

            public State(double size, Random random)
            {
                Size = size;
                Random = random;
            }
        }

        static class Cache<T>
        {
            public static readonly Generator<T> Default = Constant(default(T));
            public static readonly Generator<T[]> Empty = Constant(Array.Empty<T>());
        }

        static class EnumCache<T> where T : struct, Enum
        {
            public static readonly Generator<T> Any = Enum.GetValues(typeof(T)).OfType<T>().Select(Constant).Any();
        }

        public static readonly Generator<char> Letter = Any(Character('A', 'Z'), Character('a', 'z'));
        public static readonly Generator<char> Digit = Character('0', '9');
        public static readonly Generator<char> ASCII = Any(Letter, Digit, Character((char)0, (char)127));
        public static readonly Generator<bool> True = Constant(true);
        public static readonly Generator<bool> False = Constant(false);
        public static readonly Generator<bool> Boolean = Any(True, False);

        public static Generator<T> Default<T>() => Cache<T>.Default;
        public static Generator<T[]> Empty<T>() => Cache<T>.Empty;
        public static Generator<T> Constant<T>(T value) => _ => (value, Array.Empty<Generator<T>>());

        public static Generator<T> Lazy<T>(Func<T> provide)
        {
            var value = new Lazy<T>(provide);
            return _ => (value.Value, Array.Empty<Generator<T>>());
        }

        public static Generator<T> Lazy<T>(Func<Generator<T>> provide)
        {
            var generator = new Lazy<Generator<T>>(provide);
            return state => generator.Value(state);
        }

        public static Generator<T> Size<T>(this Generator<T> generator, Func<double, double> map) =>
            state => generator(new State(map(state.Size), state.Random));

        public static Generator<T> Enumeration<T>() where T : struct, Enum => EnumCache<T>.Any;

        public static Generator<char> Character(char minimum = char.MinValue, char maximum = char.MaxValue) =>
            Number(minimum, maximum).Map(value => (char)value).Size(_ => 1.0);

        public static Generator<int> Integer(int minimum = int.MinValue, int maximum = int.MaxValue) =>
            Number(minimum, maximum).Map(value => (int)value).Size(_ => 1.0);

        public static Generator<float> Rational(float minimum = float.MinValue, float maximum = float.MaxValue) =>
            Number(minimum, maximum, (maximum - minimum) / 100.0).Map(value => (float)value);

        public static Generator<int> Range(int maximum) => Range(0, maximum);
        public static Generator<int> Range(int minimum, int maximum) =>
            Number(minimum, maximum).Map(value => (int)value);

        public static Generator<T> Shrink<T>(this Generator<T> generator, Func<T, IEnumerable<Generator<T>>> shrink) =>
            state =>
            {
                var (value, shrinked) = generator(state);
                return (value, shrink(value).Concat(shrinked));
            };

        public static Generator<T> Shrink<T>(this Generator<T> generator, Func<T, IEnumerable<T>> shrink) =>
            generator.Shrink(value => shrink(value).Select(Constant));

        public static Generator<T> Shrink<T>(this Generator<T> generator, Func<T, Generator<T>> shrink) =>
            generator.Shrink(value => new[] { shrink(value) });

        public static Generator<string> String(int count) => Character().String(count);
        public static Generator<string> String(Generator<int> count) => Character().String(count);
        public static Generator<string> String(this Generator<char> character, int count) =>
            character.Repeat(count).Map(characters => new string(characters));
        public static Generator<string> String(this Generator<char> character, Generator<int> count) =>
            character.Repeat(count).Map(characters => new string(characters));

        public static Generator<T[]> Repeat<T>(this Generator<T> generator, int count) =>
            Enumerable.Repeat(generator, count).All();

        public static Generator<T[]> Repeat<T>(this Generator<T> generator, Generator<int> count) =>
            count.Bind(generator.Repeat);

        public static Generator<TTarget> Map<TSource, TTarget>(this Generator<TSource> generator, Func<TSource, TTarget> map) =>
            state =>
            {
                var (value, shrinked) = generator(state);
                return (map(value), shrinked.Select(shrink => shrink.Map(map)));
            };

        public static Generator<T> Flatten<T>(this Generator<Generator<T>> generator) =>
            state => generator(state).value(state);
        // {
        // var (value1, shrinked1) = generator(state);
        // var (value2, shrinked2) = value1(state);
        // return (value2, shrinked1.Select(shrink => shrink.Flatten()).Concat(shrinked2));
        // };

        public static Generator<TTarget> Bind<TSource, TTarget>(this Generator<TSource> generator, Func<TSource, Generator<TTarget>> bind) =>
            generator.Map(bind).Flatten();

        public static Generator<T> Any<T>(this IEnumerable<Generator<T>> generators) => Any(generators.ToArray());
        public static Generator<T> Any<T>(params Generator<T>[] generators) =>
            generators.Length == 0 ? throw new ArgumentException(nameof(generators)) :
            generators.Length == 1 ? generators[0] :
            state =>
            {
                var index = state.Random.Next(generators.Length);
                return generators[index](state);
            };
        public static Generator<T> Any<T>(params (float weight, Generator<T> generator)[] generators)
        {
            if (generators.Length == 0) throw new ArgumentException(nameof(generators));
            if (generators.Length == 1) return generators[0].generator;

            var sum = generators.Sum(pair => pair.weight);
            return state =>
            {
                var random = state.Random.NextDouble() * sum;
                var current = 0d;
                return generators.First(pair => random < (current += pair.weight)).generator(state);
            };
        }

        public static Generator<(T1, T2)> And<T1, T2>(this Generator<T1> generator1, Generator<T2> generator2) =>
            generator1.Bind(value1 => generator2.Map(value2 => (value1, value2)));

        public static Generator<T[]> All<T>(this IEnumerable<Generator<T>> generators) => All(generators.ToArray());
        public static Generator<T[]> All<T>(params Generator<T>[] generators) =>
            generators.Length == 0 ? Empty<T>() :
            generators.Length == 1 ? generators[0].Map(value => new[] { value }) :
            state =>
            {
                var values = new T[generators.Length];
                var shrinked = new IEnumerable<Generator<T>>[generators.Length];
                for (int i = 0; i < generators.Length; i++)
                    (values[i], shrinked[i]) = generators[i](state);

                IEnumerable<Generator<T[]>> Shrink()
                {
                    for (int i = 0; i < generators.Length; i++) yield return All(generators.RemoveAt(i));

                    var values = CloneUtility.Shallow(generators);
                    var enumerators = shrinked.Select(enumerable => enumerable.GetEnumerator());
                    for (int i = 0; i < enumerators.Length; i++)
                    {
                        var enumerator = enumerators[i];
                        while (enumerator.MoveNext())
                        {
                            values[i] = enumerator.Current;
                            yield return All(CloneUtility.Shallow(values));
                        }
                    }
                }

                return (values, Shrink());
            };

        public static Option<(T fail, T shrink)> Check<T>(this Generator<T> generator, Func<T, bool> property, int iterations = 1000, Action<T> onGenerate = null, Action<T> onShrink = null)
        {
            var random = new Random();
            for (var i = 0; i <= iterations; i++)
            {
                var seed = random.Next() ^ Thread.CurrentThread.ManagedThreadId ^ i;
                var size = i / (double)iterations;
                var state = new State(size, new Random(seed));
                var (value, shrinked) = generator(state);
                onGenerate?.Invoke(value);
                if (property(value)) continue;

                T Shrink(T value, IEnumerable<Generator<T>> shrinked)
                {
                    var @continue = true;
                    while (@continue.Change(false))
                    {
                        foreach (var generator in shrinked)
                        {
                            var state = new State(size, new Random(seed));
                            var pair = generator(state);
                            if (property(pair.value)) continue;
                            (value, shrinked) = pair;
                            onShrink?.Invoke(value);
                            @continue = true;
                            break;
                        }
                    }
                    return value;
                }

                return (value, Shrink(value, shrinked));
            }

            return Option.None();
        }

        static Generator<long> Number(long minimum, long maximum) =>
            minimum == maximum || minimum > maximum || maximum < minimum ? Constant(minimum) :
            state =>
            {
                static IEnumerable<Generator<long>> Shrink(long minimum, long maximum)
                {
                    while (true)
                    {
                        var middle = (maximum - minimum) / 2L + minimum;
                        yield return Constant(middle).Shrink(value => Shrink(minimum, middle));
                        if (!minimum.Change(middle)) break;
                    }
                }

                var value = (long)Math.Round((maximum - minimum) * state.Size * state.Random.NextDouble() + minimum);
                return (value, Shrink(minimum, value));
            };

        static Generator<double> Number(double minimum, double maximum, double decrement) =>
            minimum == maximum || minimum > maximum || maximum < minimum ? Constant(minimum) :
            state =>
            {
                static IEnumerable<Generator<double>> Shrink(double minimum, double maximum, double decrement)
                {
                    for (var i = maximum - decrement; i >= minimum; i -= decrement)
                        yield return Constant(i).Shrink(value => Shrink(minimum, maximum, decrement));
                }

                var value = (maximum - minimum) * state.Random.NextDouble() + minimum;
                return (value, Shrink(minimum, value, decrement));
            };
    }
}