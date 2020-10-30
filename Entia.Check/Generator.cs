using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Entia.Core;

namespace Entia.Check
{
    public delegate (T value, IEnumerable<Generator<T>> shrinked) Generator<T>(Generator.State state);

    public static class Generator
    {
        public sealed class State
        {
            public readonly double Size;
            public readonly uint Depth;
            public readonly Random Random;

            public State(double size, uint depth, Random random)
            {
                Size = size;
                Depth = depth;
                Random = random;
            }

            public State With(double? size = null, uint? depth = null) =>
                new State(size ?? Size, depth ?? Depth, Random);
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

        public static readonly Generator<char> Letter = Any(Range('A', 'Z'), Range('a', 'z'));
        public static readonly Generator<char> Digit = Range('0', '9');
        public static readonly Generator<char> ASCII = Any(Letter, Digit, Range((char)127));
        public static readonly Generator<char> Character = Range(char.MinValue, char.MaxValue);
        public static readonly Generator<bool> True = Constant(true);
        public static readonly Generator<bool> False = Constant(false);
        public static readonly Generator<bool> Boolean = Any(True, False);
        public static readonly Generator<int> Zero = Constant(0);
        public static readonly Generator<int> One = Constant(1);
        public static readonly Generator<int> Integer = Range(int.MinValue, int.MaxValue).Size(size => Math.Pow(size, 5));
        public static readonly Generator<float> Rational = Range(-1E10f, 1E10f).Size(size => Math.Pow(size, 10));

        public static Generator<T> Default<T>() => Cache<T>.Default;
        public static Generator<T[]> Empty<T>() => Cache<T>.Empty;

        public static Generator<T> Constant<T>(T value, IEnumerable<Generator<T>> shrinked) => _ => (value, shrinked);
        public static Generator<T> Constant<T>(T value) => Constant(value, Array.Empty<Generator<T>>());

        public static Generator<T> Lazy<T>(Func<T> provide) => Lazy(() => Constant(provide()));
        public static Generator<T> Lazy<T>(Func<Generator<T>> provide)
        {
            var generator = new Lazy<Generator<T>>(provide);
            return state => generator.Value(state);
        }

        public static Generator<T> Size<T>(this Generator<T> generator, Func<double, double> map) =>
            state => generator(state.With(map(state.Size)));
        public static Generator<T> Depth<T>(this Generator<T> generator) => state =>
            generator(state.With(depth: state.Depth + 1));
        public static Generator<T> Attenuate<T>(this Generator<T> generator, uint depth) => state =>
            generator(state.With(state.Size * Math.Max(1.0 - (double)state.Depth / depth, 0.0)));

        public static Generator<T> Enumeration<T>() where T : struct, Enum => EnumCache<T>.Any;
        public static Generator<Enum> Enumeration(Type type) => Enum.GetValues(type).OfType<Enum>().Select(Constant).Any();

        public static Generator<char> Range(char maximum) => Range('\0', maximum);
        public static Generator<char> Range(char minimum, char maximum) =>
            Number(minimum, maximum).Map(value => (char)value);
        public static Generator<float> Range(float maximum) => Range(0f, maximum);
        public static Generator<float> Range(float minimum, float maximum) =>
            Number(minimum, maximum).Map(value => (float)value);
        public static Generator<int> Range(int maximum) => Range(0, maximum);
        public static Generator<int> Range(int minimum, int maximum) =>
            Number(minimum, maximum).Map(value => (int)value);

        public static Generator<string> String(int count) => Character.String(count);
        public static Generator<string> String(Generator<int> count) => Character.String(count);
        public static Generator<string> String(this Generator<char> character, int count) =>
            character.Repeat(count).Map(characters => new string(characters));
        public static Generator<string> String(this Generator<char> character, Generator<int> count) =>
            character.Repeat(count).Map(characters => new string(characters));

        public static Generator<T[]> Repeat<T>(this Generator<T> generator, int count) =>
            count == 0 ? Cache<T>.Empty : generator.Repeat(Constant(count));
        public static Generator<T[]> Repeat<T>(this Generator<T> generator, Generator<int> count) => state =>
        {
            var length = count(state).value;
            if (length == 0) return Cache<T>.Empty(state);

            var values = new T[length];
            var shrinked = new IEnumerable<Generator<T>>[length];
            for (int i = 0; i < length; i++) (values[i], shrinked[i]) = generator(state);
            return (values, ShrinkRepeat(values, shrinked));
        };

        public static Generator<TTarget> Map<TSource, TTarget>(this Generator<TSource> generator, Func<TSource, TTarget> map) =>
            state =>
            {
                var (value, shrinked) = generator(state);
                return (map(value), shrinked.Select(shrink => shrink.Map(map)));
            };

        public static Generator<T> Any<T>(params T[] values) => Any(values.Select(Constant));
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

        public static Generator<object> Box<T>(this Generator<T> generator) => generator.Map(value => (object)value);

        public static Generator<(T1, T2)> And<T1, T2>(this Generator<T1> generator1, Generator<T2> generator2) =>
            All(generator1.Box(), generator2.Box()).Map(values => ((T1)values[0], (T2)values[1]));
        public static Generator<(T1, T2, T3)> And<T1, T2, T3>(this Generator<T1> generator1, Generator<T2> generator2, Generator<T3> generator3) =>
            All(generator1.Box(), generator2.Box(), generator3.Box()).Map(values => ((T1)values[0], (T2)values[1], (T3)values[2]));

        public static Generator<T[]> All<T>(this IEnumerable<Generator<T>> generators) => All(generators.ToArray());
        public static Generator<T[]> All<T>(params Generator<T>[] generators) =>
            generators.Length == 0 ? Empty<T>() :
            state =>
            {
                var values = new T[generators.Length];
                var shrinked = new IEnumerable<Generator<T>>[generators.Length];
                for (int i = 0; i < generators.Length; i++) (values[i], shrinked[i]) = generators[i](state);
                return (values, ShrinkAll(values, shrinked));
            };

        public static IEnumerable<T> Sample<T>(this Generator<T> generator, int count)
        {
            var random = new Random();
            for (int i = 0; i < count; i++)
            {
                var seed = random.Next() ^ Thread.CurrentThread.ManagedThreadId ^ i;
                var size = i / (double)count;
                var state = new Generator.State(size, 0, new Random(seed));
                yield return generator(state).value;
            }
        }

        static double Interpolate(double source, double target, double ratio) => (target - source) * ratio + source;

        static Generator<long> Number(long minimum, long maximum)
        {
            if (minimum == maximum || minimum > maximum || maximum < minimum) return Constant(minimum);

            var target = maximum < 0L ? maximum : minimum > 0L ? minimum : 0L;
            return state =>
            {
                var random = Interpolate(minimum, maximum, state.Random.NextDouble());
                var value = (long)Math.Round(Interpolate(random, target, 1.0 - state.Size));
                return (value, ShrinkNumber(value, target));
            };
        }

        static Generator<double> Number(double minimum, double maximum)
        {
            if (minimum == maximum || minimum > maximum || maximum < minimum) return Constant(minimum);

            var target = maximum < 0.0 ? maximum : minimum > 0.0 ? minimum : 0.0;
            return state =>
            {
                var random = Interpolate(minimum, maximum, state.Random.NextDouble());
                var value = Interpolate(random, target, 1.0 - state.Size);
                return (value, ShrinkNumber(value, target));
            };
        }

        static IEnumerable<Generator<long>> ShrinkNumber(long source, long target)
        {
            static long Round(double value)
            {
                var positive = Math.Abs(value);
                var integer = (long)positive;
                var fraction = positive - integer;
                var add = fraction < 0.5 ? 0L : 1L;
                return integer + add;
            }

            var difference = target - source;
            var sign = Math.Sign(difference);
            var magnitude = Math.Abs(difference);
            var direction = Math.Max(1L, magnitude / 100L);
            for (int i = 0; i < 100 && magnitude > 0; i++, magnitude -= direction)
            {
                var middle = Round(magnitude / 2.0) * sign + source;
                if (middle == source) yield break;
                yield return Constant(middle, ShrinkNumber(middle, target));
            }
        }

        static IEnumerable<Generator<double>> ShrinkNumber(double source, double target)
        {
            var difference = target - source;
            var sign = Math.Sign(difference);
            var magnitude = Math.Abs(difference);
            var direction = magnitude / 100.0;
            for (int i = 0; i < 100 && magnitude > 0; i++, magnitude -= direction)
            {
                var middle = Math.Round(magnitude * 0.5 * sign + source, 9);
                if (middle == source) yield break;
                yield return Constant(middle, ShrinkNumber(middle, target));
            }
        }

        static IEnumerable<Generator<T[]>> ShrinkRepeat<T>(T[] values, IEnumerable<Generator<T>>[] shrinked)
        {
            // Try to remove irrelevant generators.
            for (int i = 0; i < values.Length; i++)
            {
                var pair = (values.RemoveAt(i), shrinked.RemoveAt(i));
                yield return Constant(pair.Item1, ShrinkRepeat(pair.Item1, pair.Item2));
            }
            // Try to shrink relevant generators.
            foreach (var generator in ShrinkAll(values, shrinked)) yield return generator;
        }

        static IEnumerable<Generator<T[]>> ShrinkAll<T>(T[] values, IEnumerable<Generator<T>>[] shrinked)
        {
            for (int i = 0; i < shrinked.Length; i++)
            {
                foreach (var shrink in shrinked[i])
                {
                    yield return new Generator<T[]>(state =>
                    {
                        var pair = (CloneUtility.Shallow(values), CloneUtility.Shallow(shrinked));
                        (pair.Item1[i], pair.Item2[i]) = shrink(state);
                        return (pair.Item1, ShrinkAll(pair.Item1, pair.Item2));
                    });
                }
            }
        }
    }
}