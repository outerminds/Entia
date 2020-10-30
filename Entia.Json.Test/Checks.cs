using System;
using System.Globalization;
using System.Linq;
using Entia.Check;
using Entia.Core;
using static Entia.Check.Generator;

namespace Entia.Json.Test
{
    public static class Checks
    {
        enum Boba { A, B, C, D, E, F, G, H, I, J }

        static readonly Settings _settings = Settings.Default.With(Features.All);

        static readonly Generator<Node> _boolean = Any(
            Constant(Node.True),
            Constant(Node.False),
            Generator.Boolean.Map(Node.Boolean));
        static readonly Generator<Node> _number = Any(
            Constant(Node.Zero),
            Integer.Map(Node.Number),
            Character.Map(Node.Number),
            Rational.Map(Node.Number),
            Rational.Map(value => Node.Number(1f / value)),
            Enumeration<Boba>().Map(value => Node.Number(value)));
        static readonly Generator<Node> _string = Any(
            Constant(Node.EmptyString),
            Any(ASCII, Letter, Digit, Any('\\', '\"', '/', '\t', '\f', '\b', '\n', '\r'), Character).String(Range(100)).Map(Node.String),
            Enumeration<Boba>().Map(value => Node.String(value)));
        static readonly Generator<Node> _root = Any(
            Constant(Node.Null),
            Constant(Node.EmptyArray),
            Constant(Node.EmptyObject),
            _boolean,
            _string,
            _number,
            Lazy(() => _array.Depth()),
            Lazy(() => _object.Depth()));
        static readonly Generator<Node> _array = _root.Repeat(Range(10).Attenuate(10)).Map(Node.Array);
        static readonly Generator<Node> _object = All(_string, _root).Repeat(Range(10).Attenuate(10)).Map(nodes => Node.Object(nodes.Flatten()));

        static readonly Generator<(object, string, Result<object>)> _default = ReflectionUtility.AllTypes
            .Where(type => type.IsPublic && !type.IsGenericTypeDefinition && !type.IsAbstract)
            .Where(type =>
                type.IsPrimitive ||
                type.IsEnum ||
                type.IsSerializable ||
                type.Namespace.Contains(nameof(Entia)) ||
                type.Namespace.Contains($"{nameof(System)}.{nameof(System.IO)}") ||
                type.Namespace.Contains($"{nameof(System)}.{nameof(System.Text)}") ||
                type.Namespace.Contains($"{nameof(System)}.{nameof(System.Linq)}") ||
                type.Namespace.Contains($"{nameof(System)}.{nameof(System.Buffers)}") ||
                type.Namespace.Contains($"{nameof(System)}.{nameof(System.Data)}") ||
                type.Namespace.Contains($"{nameof(System)}.{nameof(System.Collections)}") ||
                type.Namespace.Contains($"{nameof(System)}.{nameof(System.ComponentModel)}") ||
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
        static readonly Generator<(Node, string, Node)> _nested = _root
            .Map(node =>
            {
                string Generate(Node node) => Serialization.Generate(node.Map(child => Generate(child)));
                Node Parse(string json) => Serialization.Parse(json).Or(Node.Null).Map(child => Parse(child.AsString()));
                var generated = Generate(node);
                var parsed = Parse(generated);
                return (node, generated, parsed);
            });
        static readonly Generator<(Node, string, Result<Node>, Node)> _rational = _number.Map(node =>
            {
                var generated = Serialization.Generate(node);
                var parsed = Serialization.Parse(generated);
                return (node, generated, parsed, node.IsNull() ? node : Node.Number(double.Parse(generated, CultureInfo.InvariantCulture)));
            });

        public static void Run()
        {
            _string.Check("Generate/parse symmetry for String nodes.");
            _number.Check("Generate/parse symmetry for Number nodes.");
            _rational.Check("Generate/parse/double.Parse symmetry for Number nodes.", tuple =>
                tuple.Item1 == tuple.Item3 &&
                tuple.Item1 == tuple.Item4 &&
                tuple.Item3 == tuple.Item4);
            _root.Check("Generate/parse symmetry for Root nodes.");
            _nested.Check("Generate/parse symmetry for nested json.", tuple => tuple.Item1 == tuple.Item3);
            _default.Check("Serialize/deserialize abstract instances to same type.", tuple =>
                tuple.Item1 != null &&
                tuple.Item3.TryValue(out var value) &&
                value != null &&
                tuple.Item1.GetType() == value.GetType());

            // TODO: Add test for parsing foreign jsons
            // TODO: Add test for comparing output with Json.Net and .Net Json parser.
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