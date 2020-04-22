using System.Collections.Generic;

const string phase = "TPhase";
const string message = "TMessage";

IEnumerable<(string declaration, string run1, string run2)> Generate(int depth)
{
    static IEnumerable<string> GenericParameters(int count)
    {
        if (count == 1) yield return "T";
        else for (var i = 1; i <= count; i++) yield return $"T{i}";
    }

    static string Declaration(string suffix, IEnumerable<string> generics, IEnumerable<string> parameters, IEnumerable<string> constraints)
    {
        var runGenerics = generics.ToArray();
        var runEach = $"RunEach{suffix}{(runGenerics.Length == 0 ? "" : $"<{string.Join(", ", runGenerics)}>")}";
        return $"public delegate void {runEach}({string.Join(", ", parameters)}) {string.Join(" ", constraints)};";
    }

    static string Body1(string suffix, bool hasEntity, bool hasPhase, IEnumerable<string> generics, IEnumerable<string> constraints)
    {
        IEnumerable<string> Arguments()
        {
            if (hasPhase) yield return "phase";
            if (hasEntity) yield return "entities[i]";
        }

        var parameters = generics.Any() ? $"<{string.Join(", ", generics)}>" : "";
        var runGenerics = generics;
        if (hasPhase) runGenerics = runGenerics.Prepend(phase);
        var runParameters = runGenerics.Any() ? $"<{string.Join(", ", runGenerics)}>" : "";
        var storeVars = generics.Select((generic, index) => $"var store{index + 1} = segment.Store<{generic}>();");
        var storeRefs = generics.Select((generic, index) => $"ref store{index + 1}[i]");
        var dependencies = generics.Any() ? string.Join(", ", generics.Select(generic => $"new Write(typeof({generic}))")) : "Array.Empty<IDependency>()";
        var arguments = Arguments().Concat(storeRefs);
        var forRun = $"for (int i = 0; i < count; i++) run({string.Join(", ", arguments)});";
        var has = generics.Select(generic => $"Has<{generic}>()").Append("filter ?? True");

        return
$@"public static Node RunEach{parameters}(RunEach{suffix}{runParameters} run, Filter? filter = null) {string.Join(" ", constraints)} => RunEach(
    segment => (in {phase} phase) =>
    {{
        var (entities, count) = segment.Entities;
        {string.Join(" ", storeVars)}
        {forRun}
    }},
    All({string.Join(", ", has)}), {dependencies});";
    }

    static string Body2(string suffix, bool hasEntity, bool hasMessage, bool hasPhase, IEnumerable<string> generics, IEnumerable<string> constraints)
    {
        IEnumerable<string> Arguments()
        {
            if (hasPhase) yield return "phase";
            if (hasMessage) yield return "message";
            if (hasEntity) yield return "entities[i]";
        }

        var parameters = generics.Any() ? $"<{string.Join(", ", generics)}>" : "";
        var runGenerics = generics;
        if (hasMessage) runGenerics = runGenerics.Prepend(message);
        if (hasPhase) runGenerics = runGenerics.Prepend(phase);
        var runParameters = runGenerics.Any() ? $"<{string.Join(", ", runGenerics)}>" : "";
        var storeVars = generics.Select((generic, index) => $"var store{index + 1} = segment.Store<{generic}>();");
        var storeRefs = generics.Select((generic, index) => $"ref store{index + 1}[i]");
        var dependencies = generics.Any() ? string.Join(", ", generics.Select(generic => $"new Write(typeof({generic}))")) : "Array.Empty<IDependency>()";
        var arguments = Arguments().Concat(storeRefs);
        var forRun = $"for (int i = 0; i < count; i++) run({string.Join(", ", arguments)});";
        var has = generics.Select(generic => $"Has<{generic}>()").Append("filter ?? True");

        return
$@"public static Node RunEach{parameters}(RunEach{suffix}{runParameters} run, Filter? filter = null, int? capacity = null) {string.Join(" ", constraints)} => RunEach(
    segment => (in {phase} phase, in {message} message) =>
    {{
        var (entities, count) = segment.Entities;
        {string.Join(" ", storeVars)}
        {forRun}
    }},
    All({string.Join(", ", has)}), capacity, {dependencies});";
    }

    for (int i = 0; i <= depth; i++)
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
            Declaration("P",
                generics.Prepend(phase),
                parameters.Prepend($"in {phase} phase"),
                constraints.Prepend($"where {phase} : struct, IMessage")),
            Body1("P", false, true, generics, constraints),
            Body2("P", false, false, true, generics, constraints));
        yield return (
            Declaration("PE",
                generics.Prepend(phase),
                parameters.Prepend("Entity entity").Prepend($"in {phase} phase"),
                constraints.Prepend($"where {phase} : struct, IMessage")),
            Body1("PE", true, true, generics, constraints),
            Body2("PE", true, false, true, generics, constraints));

        yield return (
            Declaration("M",
                generics.Prepend(message),
                parameters.Prepend($"in {message} message"),
                constraints.Prepend($"where {message} : struct, IMessage")),
            "",
            Body2("M", false, true, false, generics, constraints));
        yield return (
            Declaration("ME",
                generics.Prepend(message),
                parameters.Prepend("Entity entity").Prepend($"in {message} message"),
                constraints.Prepend($"where {message} : struct, IMessage")),
            "",
            Body2("ME", true, true, false, generics, constraints));

        yield return (
            Declaration("PM",
                generics.Prepend($"{message}").Prepend($"{phase}"),
                parameters.Prepend($"in {message} message").Prepend($"in {phase} phase"),
                constraints.Prepend($"where {message} : struct, IMessage").Prepend($"where {phase} : struct, IMessage")),
            "",
            Body2("PM", false, true, true, generics, constraints)
        );
        yield return (
            Declaration("PME",
                generics.Prepend($"{message}").Prepend($"{phase}"),
                parameters.Prepend("Entity entity").Prepend($"in {message} message").Prepend($"in {phase} phase"),
                constraints.Prepend($"where {message} : struct, IMessage").Prepend($"where {phase} : struct, IMessage")),
            "",
            Body2("PME", true, true, true, generics, constraints)
        );
    }
}

var file = "Node.RunEach";
var results = Generate(9).ToArray();
var code =
$@"/* DO NOT MODIFY: The content of this file has been generated by the script '{file}.csx'. */

using System;
using Entia.Dependencies;
using Entia.Experimental.Systems;
using static Entia.Experimental.Filter;

namespace Entia.Experimental
{{
    namespace Systems
    {{
{string.Join(Environment.NewLine, results.Select(result => result.declaration).Where(value => !string.IsNullOrEmpty(value)))}
    }}

    public sealed partial class Node
    {{
        public static partial class System<{phase}>
        {{
            public static partial class Receive<{message}>
            {{
{string.Join(Environment.NewLine, results.Select(result => result.run2).Where(value => !string.IsNullOrEmpty(value)))}
            }}

{string.Join(Environment.NewLine, results.Select(result => result.run1).Where(value => !string.IsNullOrEmpty(value)))}
        }}
    }}
}}";


File.WriteAllText($"./{file}.cs", code);