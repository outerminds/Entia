using System.Collections.Generic;

const string message = "TMessage";
const string react = "TReact";
const string receive = "TReceive";

IEnumerable<(string declaration, string run1, string run2)> Generate(int depth)
{

    static IEnumerable<string> GenericParameters(int count)
    {
        if (count == 1) yield return "T";
        else for (var i = 1; i <= count; i++) yield return $"T{i}";
    }

    static string Declaration(string suffix, IEnumerable<string> generics, IEnumerable<string> parameters, IEnumerable<string> constraints) =>
        $"public delegate void RunEach{suffix}<{string.Join(", ", generics)}>({string.Join(", ", parameters)}) {string.Join(" ", constraints)};";

    static string Body1(string suffix, bool hasEntity, bool hasMessage, IEnumerable<string> generics, IEnumerable<string> constraints)
    {
        IEnumerable<string> Arguments()
        {
            if (hasMessage) yield return "message";
            if (hasEntity) yield return "entities[i]";
        }

        var runGenerics = hasMessage ? generics.Prepend(react) : generics;
        var runEach = $"RunEach{suffix}<{string.Join(", ", runGenerics)}>";
        var storeVars = generics.Select((generic, index) => $"var store{index + 1} = segment.Store<{generic}>();");
        var storeRefs = generics.Select((generic, index) => $"ref store{index + 1}[i]");
        var dependencies = generics.Select(generic => $"new Write(typeof({generic}))");
        var arguments = Arguments().Concat(storeRefs);
        var forRun = $"for (int i = 0; i < count; i++) run({string.Join(", ", arguments)});";

        return
$@"public static Node RunEach<{string.Join(", ", generics)}>({runEach} run, Filter? filter = null) {string.Join(" ", constraints)} => RunEach(
    segment => (in {react} message) =>
    {{
        var (entities, count) = segment.Entities;
        {string.Join(" ", storeVars)}
        {forRun}
    }},
    Filter.All<{string.Join(", ", generics)}>(filter ?? Filter.True),
    {string.Join(", ", dependencies)});";
    }

    static string Body2(string suffix, bool hasEntity, bool hasReceive, bool hasReact, IEnumerable<string> generics, IEnumerable<string> constraints)
    {
        IEnumerable<string> Arguments()
        {
            if (hasReact) yield return "react";
            if (hasReceive) yield return "receive";
            if (hasEntity) yield return "entities[i]";
        }

        var runGenerics = generics;
        if (hasReceive) runGenerics = runGenerics.Prepend(receive);
        if (hasReact) runGenerics = runGenerics.Prepend(react);
        var runEach = $"RunEach{suffix}<{string.Join(", ", runGenerics)}>";
        var storeVars = generics.Select((generic, index) => $"var store{index + 1} = segment.Store<{generic}>();");
        var storeRefs = generics.Select((generic, index) => $"ref store{index + 1}[i]");
        var dependencies = generics.Select(generic => $"new Write(typeof({generic}))");
        var arguments = Arguments().Concat(storeRefs);
        var forRun = $"for (int i = 0; i < count; i++) run({string.Join(", ", arguments)});";

        return
$@"public static Node RunEach<{string.Join(", ", generics)}>({runEach} run, Filter? filter = null, int? capacity = null) {string.Join(" ", constraints)} => RunEach(
    segment => (in {react} react, in {receive} receive) =>
    {{
        var (entities, count) = segment.Entities;
        {string.Join(" ", storeVars)}
        {forRun}
    }},
    Filter.All<{string.Join(", ", generics)}>(filter ?? Filter.True), capacity, {string.Join(", ", dependencies)});";
    }

    for (int i = 1; i <= depth; i++)
    {
        var generics = GenericParameters(i).ToArray();
        var parameters = generics.Select((generic, index) => $"ref {generic} component{index + 1}");
        var constraints = generics.Select((generic, index) => $"where {generic} : struct, IComponent");

        yield return (
            Declaration("", generics, parameters, constraints),
            Body1("", false, false, generics, constraints),
            Body2("", false, false, false, generics, constraints));
        yield return (
            Declaration("E", generics, parameters.Prepend("Entity entity"), constraints),
            Body1("E", true, false, generics, constraints),
            Body2("E", true, false, false, generics, constraints));
        yield return (
            Declaration("M",
                generics.Prepend(message),
                parameters.Prepend($"in {message} message"),
                constraints.Prepend($"where {message} : struct, IMessage")),
            Body1("M", false, true, generics, constraints),
            Body2("M", false, true, false, generics, constraints));
        yield return (
            Declaration("ME",
                generics.Prepend(message),
                parameters.Prepend("Entity entity").Prepend($"in {message} message"),
                constraints.Prepend($"where {message} : struct, IMessage")),
            Body1("ME", true, true, generics, constraints),
            Body2("ME", true, true, false, generics, constraints));
        yield return (
            Declaration("MM",
                generics.Prepend($"{message}2").Prepend($"{message}1"),
                parameters.Prepend($"in {message}2 message2").Prepend($"in {message}1 message1"),
                constraints.Prepend($"where {message}2 : struct, IMessage").Prepend($"where {message}1 : struct, IMessage")),
            "",
            Body2("MM", false, true, true, generics, constraints)
        );
        yield return (
            Declaration("MME",
                generics.Prepend($"{message}2").Prepend($"{message}1"),
                parameters.Prepend("Entity entity").Prepend($"in {message}2 message2").Prepend($"in {message}1 message1"),
                constraints.Prepend($"where {message}2 : struct, IMessage").Prepend($"where {message}1 : struct, IMessage")),
            "",
            Body2("MME", true, true, true, generics, constraints)
        );
    }
}

var file = "Node.RunEach";
var results = Generate(5).ToArray();
var code =
$@"/* DO NOT MODIFY: The content of this file has been generated by the script '{file}.csx'. */

using Entia.Dependencies;
using Entia.Experimental.Systems;

namespace Entia.Experimental
{{
    namespace Systems
    {{
{string.Join(Environment.NewLine, results.Select(result => result.declaration).Where(value => !string.IsNullOrEmpty(value)))}
    }}

    public readonly partial struct Node
    {{
        public static partial class When<{react}>
        {{
{string.Join(Environment.NewLine, results.Select(result => result.run1).Where(value => !string.IsNullOrEmpty(value)))}
        }}

        public static partial class When<{react}, {receive}>
        {{
{string.Join(Environment.NewLine, results.Select(result => result.run2).Where(value => !string.IsNullOrEmpty(value)))}
        }}
    }}
}}";


File.WriteAllText($"./{file}.cs", code);