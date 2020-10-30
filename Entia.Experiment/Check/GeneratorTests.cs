using System;
using System.Linq;
using System.Threading.Tasks;
using Entia.Core;
using Entia.Json;
using Entia.Check;
using static Entia.Check.Generator;

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
                Integer.Map(Node.Number),
                Character.Map(Node.Number),
                Rational.Map(Node.Number),
                Range(-1f, -float.Epsilon).Map(Node.Number), // Small negative numbers.
                Range(float.Epsilon, 1f).Map(Node.Number), // Small positive numbers.
                Enumeration<Boba>().Map(value => Node.Number(value)));
            public static Generator<Node> String() => Any(
                Constant(Node.EmptyString),
                Any(ASCII, Letter, Digit, Any('\\', '\"', '/', '\t', '\f', '\b', '\n', '\r'), Character)
                    .String(Range(100))
                    .Map(Node.String),
                Enumeration<Boba>().Map(value => Node.String(value)));
            public static Generator<Node> Leaf() => Any(
                Constant(Node.Null),
                Constant(Node.EmptyArray),
                Constant(Node.EmptyObject),
                Boolean(),
                String(),
                Number());
            public static Generator<Node> Branch() => Any(Lazy(Array), Lazy(Object)).Depth();
            public static Generator<Node> Array() => Root().Repeat(Range(10).Attenuate(10)).Map(Node.Array);
            public static Generator<Node> Object() => String().And(Root()).Repeat(Range(10).Attenuate(10))
                .Map(pairs => Node.Object(pairs.SelectMany(pair => new[] { pair.Item1, pair.Item2 }).ToArray()));
            public static Generator<Node> Root() => Any((Leaf()), Branch());
        }

        enum Boba { A, B, C, D, E, F, G, H, I, J }

        static readonly Settings _settings = Settings.Default.With(Features.All);
        static readonly Generator<(object, string, Result<object>)> _default = ReflectionUtility.AllTypes
            .Where(type => type.IsPublic && !type.IsGenericTypeDefinition && !type.IsAbstract)
            .Where(type =>
                type.IsPrimitive ||
                type.IsEnum ||
                type.IsSerializable ||
                type.Namespace.Contains(nameof(Entia)) ||
                type.Namespace.Contains($"{nameof(System)}.{nameof(System.Collections)}") ||
                type.Namespace.Contains($"{nameof(System)}.{nameof(System.Numerics)}"))
            .Select(type => type.DefaultConstructor()
                .Bind(constructor => Option.Try(() => constructor.Invoke(Array.Empty<object>()))))
            .Choose()
            .Select(Constant)
            .Any()
            .Map(value =>
            {
                var json = Serialization.Serialize(value, _settings);
                var result = Serialization.Deserialize<object>(json, _settings);
                return (value, json, result);
            });
        static readonly Generator<(Node, string, Node)> _nested = Json.Root()
            .Map(node =>
            {
                string Generate(Node node) => Serialization.Generate(node.Map(child => Generate(child)));
                Node Parse(string json) => Serialization.Parse(json).Or(Node.Null).Map(child => Parse(child.AsString()));
                var generated = Generate(node);
                var parsed = Parse(generated);
                return (node, generated, parsed);
            });

        public static void Test()
        {
            var failures = _default.Check("Serialize/deserialize abstract instances to same type.", tuple =>
                tuple.Item1 != null &&
                tuple.Item3.TryValue(out var value) &&
                value != null &&
                tuple.Item1.GetType() == value.GetType());
            var failures1 = Json.String().Check("Generate/parse symmetry for String nodes.");
            var failures2 = Json.Number().Check("Generate/parse symmetry for Number nodes.");
            var failures3 = Json.Leaf().Check("Generate/parse symmetry for Leaf nodes.");
            var failures4 = Json.Root().Check("Generate/parse symmetry for Root nodes.");
            var failures5 = _nested.Check("Generate/parse symmetry for nested json.", tuple => tuple.Item1 == tuple.Item3);

            // TODO: Add test for parsing foreign jsons
            // TODO: Add test for comparing output with Json.Net and .Net Json parser.
            Console.ReadLine();
        }

        public static void CharacterTests()
        {
            var failures1 = Letter.Check("Letter", char.IsLetter);
            var failures2 = Digit.Check("Digit", char.IsDigit);
            var failures3 = ASCII.Check("ASCII", value => value < 128);
        }

        public static void StringTests()
        {
            var failures1 = Letter.String(Range(100)).Check("Letter", value => value.All(char.IsLetter));
            var failures2 = Digit.String(Range(100)).Check("Digit", value => value.All(char.IsDigit));
            var failures3 = ASCII.String(Range(100)).Check("ASCII", value => value.All(value => value < 128));
        }

        public static void JsonTests()
        {
            var failures1 = Json.Number().Check("Number", node => node.TryLong(out var value) && value < 100);
            var failures2 = Json.String().Check("String", node => node.TryString(out var value) && value.Count(_ => _ == 'K') < 3);
            var failures3 = Json.Array().Check("Shallow Array", node => node.Items().Count(item => item.IsNumber()) < 5);
            var failures4 = Json.Object().Check("Shallow Object", node => node.Members().Count(pair => pair.value.IsNull()) < 5);
            var failures5 = Json.Root().Check("Deep Root", node => node.Descendants().Count(child => child.IsNull()) < 10);
        }

        static Failure<T>[] Check<T>(this Generator<T> generator, string name, Func<T, bool> prove) =>
            generator.Prove(name, prove).Log(name).Check();

        static Failure<(Node, string, Result<Node>)>[] Check(this Generator<Node> generator, string name) => generator
            .Map(node =>
            {
                var json = Serialization.Generate(node);
                var result = Serialization.Parse(json);
                return (node, json, result);
            })
            .Prove(name, values => values.node == values.result)
            .Log(name)
            .Check();
    }
}