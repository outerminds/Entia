using System;
using System.Linq;
using Entia.Core;
using Entia.Json;
using static Entia.Experiment.Check.Generator;

namespace Entia.Experiment.Check
{
    public static class GeneratorTests
    {
        enum Boba { A, B, C, D, E, F, G, H, I, J }

        public static void Test()
        {
            // CharacterTests();
            // StringTests();
            // JsonTests();
            var result1 = Character().Check(value => value < '7');
            var result2 = Letter.String(Range(100)).Check(value => value.Count(_ => _ == 'K') < 3);
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
            static Generator<Node> String() => Letter.String(Range(100)).Map(Node.String);
            static Generator<Node> Array() => Lazy(Generate).Repeat(Range(10)).Map(Node.Array);
            static Generator<Node> Object() => All(String(), Lazy(Generate))
                .Repeat(Range(10))
                .Map(pairs => Node.Object(pairs.Flatten().ToArray()));
            static Generator<Node> Generate() => Any(
                (1f, Constant(Node.Null)),
                (1f, Constant(Node.True)),
                (1f, Constant(Node.False)),
                (1f, Constant(Node.EmptyArray)),
                (1f, Constant(Node.EmptyObject)),
                (1f, Constant(Node.EmptyString)),
                (1f, Constant(Node.Zero)),
                (1f, Generator.Boolean.Map(Node.Boolean)),
                (1f, Character().Map(Node.Number)),
                (1f, String()),
                (1f, Integer().Map(Node.Number)),
                (1f, Rational().Map(Node.Number)),
                (1f, Enumeration<Boba>().Map(value => Node.Number(value))),
                (1f, Enumeration<Boba>().Map(value => Node.String(value))),
                (1f, Array()),
                (1f, Object()));

            var result1 = String().Check(
                node => node.TryString(out var value) && value.Count(_ => _ == 'K') < 3,
                1_000,
                node => Console.WriteLine($"Generate: {node}"),
                node => Console.WriteLine($"Shrink: {node}"));
            var result2 = Object().Check(
                node => node.Descendants().Count(child => child.Kind == Node.Kinds.Null) < 3,
                1_000,
                node => Console.WriteLine($"Generate: {node}"),
                node => Console.WriteLine($"Shrink: {node}"));
        }
    }
}