using System;
using System.Globalization;
using System.Linq;
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
            var node = Node.Object(Node.String('\u0101'), Node.Array());
            var generated = (Serialization.Generate(node), "{\"\\u0101\":[]}");
            var parsed = (Serialization.Parse(generated.Item1), Serialization.Parse(generated.Item2));
            var results = (node == parsed.Item1, node == parsed.Item2);

            var result1 = Json.String().Check();
            var result2 = Json.Number().Check();
            var result3 = Json.Leaf().Check();
            var result4 = Json.Root().Check();
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

        static Option<((Node node, string generated, Result<Node> parsed) fail, (Node node, string generated, Result<Node> parsed) shrinked)> Check(this Generator<Node> generator) => generator
            .Map(node =>
            {
                var generated = Serialization.Generate(node);
                var parsed = Serialization.Parse(generated);
                return (node, generated, parsed);
            })
            .Check(values => values.node == values.parsed);

        static Option<(T fail, T shrinked)> Check<T>(this Generator<T> generator, Func<T, bool> property) =>
            generator.Check(property, 100_000,
                node => Console.WriteLine($"Generate: {node}"),
                node => Console.WriteLine($"Shrink: {node}"));
    }
}