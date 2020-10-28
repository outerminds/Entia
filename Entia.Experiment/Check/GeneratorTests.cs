using System;
using System.Linq;
using System.Threading.Tasks;
using Entia.Core;
using Entia.Json;
using static Entia.Experiment.Check.Generator;

namespace Entia.Experiment.Check
{
    public static class GeneratorTests
    {
        public static class Json
        {
            public static Generator<Node> Boolean() => Any(
                Constant(Node.True),
                Constant(Node.False),
                Generator.Boolean.Map(Node.Boolean));
            public static Generator<Node> Number() => Any(
                Constant(Node.Zero),
                Integer().Map(Node.Number),
                Rational().Map(Node.Number),
                Character().Map(Node.Number),
                Enumeration<Boba>().Map(value => Node.Number(value)));
            public static Generator<Node> String() => Any(
                Constant(Node.EmptyString),
                Escaped(),
                Any(ASCII, Letter, Digit, Character()).String(Range(100)).Map(Node.String),
                Enumeration<Boba>().Map(value => Node.String(value)));
            public static Generator<Node> Escaped() => Any(
                Constant(Node.String('\\')),
                Constant(Node.String('\"')),
                Constant(Node.String('/')),
                Constant(Node.String('\t')),
                Constant(Node.String('\f')),
                Constant(Node.String('\b')),
                Constant(Node.String('\n')),
                Constant(Node.String('\r'))
            );
            public static Generator<Node> Leaf() => Any(
                Constant(Node.Null),
                Constant(Node.EmptyArray),
                Constant(Node.EmptyObject),
                Boolean(),
                String(),
                Number());
            public static Generator<Node> Branch() => Any(Lazy(Array), Lazy(Object), LeafArray(), LeafObject()).Depth(50);
            public static Generator<Node> LeafArray() => Leaf().Repeat(Range(100)).Map(Node.Array);
            public static Generator<Node> Array() => Root().Repeat(Range(10)).Map(Node.Array);
            public static Generator<(Node, Node)> Pair(Generator<Node> value) => String().And(value);
            public static Generator<Node> LeafObject() => Pair(Leaf()).Repeat(Range(100))
                .Map(pairs => Node.Object(pairs.SelectMany(pair => new[] { pair.Item1, pair.Item2 }).ToArray()));
            public static Generator<Node> Object() => Pair(Root()).Repeat(Range(10))
                .Map(pairs => Node.Object(pairs.SelectMany(pair => new[] { pair.Item1, pair.Item2 }).ToArray()));
            public static Generator<Node> Root() => Any(Leaf(), Branch());
        }

        enum Boba { A, B, C, D, E, F, G, H, I, J }

        public static void Test()
        {
            // CharacterTests();
            // StringTests();
            // JsonTests();

            var settings = Settings.Default.With(Features.All);

            // var result = ReflectionUtility.AllTypes
            //     .Where(type => type.IsPublic && !type.IsGenericTypeDefinition && !type.IsAbstract)
            //     .Where(type =>
            //         type.IsPrimitive ||
            //         type.IsEnum ||
            //         type.IsSerializable ||
            //         type.Namespace.Contains(nameof(Entia)) ||
            //         type.Namespace.Contains($"{nameof(System)}.{nameof(System.Collections)}") ||
            //         type.Namespace.Contains($"{nameof(System)}.{nameof(System.Numerics)}"))
            //     .Select(type => type.DefaultConstructor()
            //         .Bind(constructor => Option.Try(() => constructor.Invoke(Array.Empty<object>()))))
            //     .Choose()
            //     .Select(Constant)
            //     .Any()
            //     .Map(value =>
            //     {
            //         var json = Serialization.Serialize(value, settings);
            //         var result = Serialization.Deserialize<object>(json, settings);
            //         return (value, json, result);
            //     })
            //     .Check(tuple =>
            //         tuple.value != null &&
            //         tuple.result.TryValue(out var value) &&
            //         value != null &&
            //         tuple.value.GetType() == value.GetType());
            // result.TryValue(out var pair);

            var failures1 = Json.String().Check("String");
            var failures2 = Json.Number().Check("Number");
            var failures3 = Json.Leaf().Check("Leaf");
            var failures4 = Json.Root().Check("Root");
            Console.ReadLine();
        }

        public static void CharacterTests()
        {
            var result1 = Letter.Check(char.IsLetter);
            var result2 = Digit.Check(char.IsDigit);
            var result3 = ASCII.Check(value => value < 128);
        }

        public static void StringTests()
        {
            var result1 = Letter.String(Range(100)).Check(value => value.All(char.IsLetter));
            var result2 = Digit.String(Range(100)).Check(value => value.All(char.IsDigit));
            var result3 = ASCII.String(Range(100)).Check(value => value.All(value => value < 128));
        }

        public static void JsonTests()
        {
            var result1 = Json.Number().Check(node => node.TryLong(out var value) && value < 100);
            var result2 = Json.String().Check(node => node.TryString(out var value) && value.Count(_ => _ == 'K') < 3);
            var result3 = Json.LeafArray().Check(node => node.Items().Count(item => item.IsNumber()) < 5);
            var result4 = Json.LeafObject().Check(node => node.Members().Count(pair => pair.value.IsNull()) < 5);
            var result5 = Json.Root().Check(node => node.Descendants().Count(item => item.IsNull()) < 5);
        }

        static ((Node, string, Result<Node>) fail, (Node, string, Result<Node>) shrinked)[] Check(this Generator<Node> generator, string name) => generator
            .Map(node =>
            {
                var json = Serialization.Generate(node);
                var result = Serialization.Parse(json);
                return (node, json, result);
            })
            .Check(name, values => values.node == values.result, 1_000_000);

        static (T fail, T shrinked)[] Check<T>(this Generator<T> generator, string name, Func<T, bool> property, int iterations, int? parallel = null)
        {
            var tasks = parallel ?? Environment.ProcessorCount;
            var count = iterations / tasks;
            var progress = new double[tasks];
            var task = Task.WhenAll(Enumerable.Range(0, tasks).Select(index => Task.Run(() =>
                generator.Check(property, count, (value, iteration) => progress[index] = iteration / (double)count))));

            Console.CursorVisible = false;
            Console.WriteLine();

            while (!task.IsCompleted)
            {
                Console.CursorLeft = 0;
                Console.Write($"Generating '{name}' {count}x{tasks}... {progress.Average() * 100:0.00}%");
            }

            Console.WriteLine();
            var failures = task.Result.Choose().ToArray();
            if (failures.Length == 0) Console.WriteLine("Success");
            else Console.WriteLine($"Failure: {string.Join("", failures.Select(failure => $"{Environment.NewLine}-> {failure.shrink}"))}");

            Console.CursorVisible = true;
            return failures;
        }
    }
}