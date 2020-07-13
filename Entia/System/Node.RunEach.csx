using System.Collections.Generic;

const string message = "TMessage";

IEnumerable<(string declaration1, string declaration2, string run1, string run2)> Generate(int depth)
{
    static IEnumerable<string> GenericParameters(int count)
    {
        if (count == 1) yield return "T";
        else for (var i = 1; i <= count; i++) yield return $"T{i}";
    }

    static IEnumerable<string> MessageParameters(bool hasPhase)
    {
        if (hasPhase) yield return $"in {message} {nameof(message)}";
    }

    static IEnumerable<string> MessageGenericParameters(bool hasPhase)
    {
        if (hasPhase) yield return message;
    }

    static IEnumerable<string> RunArguments(bool hasPhase, bool hasArray, bool hasEntity, string[] generics)
    {
        if (hasPhase) yield return $"{nameof(message)}";

        if (hasArray && hasEntity) yield return "entities";
        else if (hasEntity) yield return "entities[i]";

        for (int i = 0; i < generics.Length; i++)
            yield return hasArray ? $"store{i + 1}" : $"ref store{i + 1}[i]";

        if (hasArray) yield return "count";
    }

    static string Suffix(bool hasPhase, bool hasArray, bool hasEntity) =>
        (hasPhase ? "P" : "") +
        (hasArray ? "A" : "") +
        (hasEntity ? "E" : "");

    static string DeclarationGenerics(IEnumerable<string> generics) =>
        generics.Any() ? $"<{string.Join(", ", generics)}>" : "";

    static IEnumerable<string> DeclarationParameters(bool hasPhase, bool hasArray, bool hasEntity, string[] generics)
    {
        if (hasPhase) yield return $"in {message} {nameof(message)}";

        if (hasArray && hasEntity) yield return "Entity[] entities";
        else if (hasEntity) yield return "Entity entity";

        for (int i = 0; i < generics.Length; i++)
            yield return hasArray ? $"{generics[i]}[] store{i + 1}" : $"ref {generics[i]} component{i + 1}";

        if (hasArray) yield return "int count";
    }

    static IEnumerable<string> MessageConstraints(string[] generics) =>
        generics.Select(generic => $"where {generic} : struct, IMessage");
    static IEnumerable<string> ComponentConstraints(string[] generics) =>
        generics.Select(generic => $"where {generic} : struct, IComponent");

    static string DelegateName(bool hasPhase, bool hasArray, bool hasEntity) =>
        $"RunEach{Suffix(hasPhase, hasArray, hasEntity)}";

    static (string declaration1, string declaration2, string run1, string run2) Declaration(bool hasPhase, bool hasEntity, string[] generics)
    {
        var runStoreName = DelegateName(hasPhase, true, hasEntity);
        var runComponentName = DelegateName(hasPhase, false, hasEntity);
        var storeParameters = DeclarationParameters(hasPhase, true, hasEntity, generics);
        var componentParameters = DeclarationParameters(hasPhase, false, hasEntity, generics);

        var messageParameters = MessageParameters(true);
        var messageGenerics = MessageGenericParameters(hasPhase).ToArray();
        var declarationGenerics = DeclarationGenerics(messageGenerics.Concat(generics));
        var runGenerics = DeclarationGenerics(generics);
        var runStoreType = $"{runStoreName}{declarationGenerics}";
        var runComponentType = $"{runComponentName}{declarationGenerics}";

        var declarationConstraints = string.Join(" ", MessageConstraints(messageGenerics).Concat(ComponentConstraints(generics)));
        var runConstraints = string.Join(" ", ComponentConstraints(generics));
        var storeVars = generics.Select((generic, index) => $"var store{index + 1} = segment.Store<{generic}>();");
        var storeRefs = generics.Select((generic, index) => $"ref store{index + 1}[i]");
        var storeArguments = RunArguments(hasPhase, true, hasEntity, generics);
        var componentArguments = RunArguments(hasPhase, false, hasEntity, generics);
        var has = generics.Select(generic => $"Has<{generic}>()").Append("filter ?? True");
        var filter = $"All({string.Join(", ", has)})";
        var dependencies = generics.Any() ? string.Join(", ", generics.Select(generic => $"new Write(typeof({generic}))")) : "Array.Empty<IDependency>()";

        return (
$@"public delegate void {runStoreName}{declarationGenerics}({string.Join(", ", storeParameters)}) {declarationConstraints};",
$@"public delegate void {runComponentName}{declarationGenerics}({string.Join(", ", componentParameters)}) {declarationConstraints};",
$@"public static Node RunEach{runGenerics}({runStoreType} run, Filter? filter = null) {runConstraints} => RunEach(
    segment => ({string.Join(", ", messageParameters)}) =>
    {{
        var (entities, count) = segment.Entities;
        {string.Join(" ", storeVars)}
        run({string.Join(", ", storeArguments)});
    }},
    {filter}, {dependencies});",
$@"public static Node RunEach{runGenerics}({runComponentType} run, Filter? filter = null) {runConstraints} => RunEach(
    segment => ({string.Join(", ", messageParameters)}) =>
    {{
        var (entities, count) = segment.Entities;
        {string.Join(" ", storeVars)}
        for (var i = 0; i < count; i++) run({string.Join(", ", componentArguments)});
    }},
    {filter}, {dependencies});");
    }

    for (int i = 0; i <= depth; i++)
    {
        var generics = GenericParameters(i).ToArray();
        yield return Declaration(true, true, generics);
        yield return Declaration(true, false, generics);
        yield return Declaration(false, true, generics);
        yield return Declaration(false, false, generics);
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
{string.Join(Environment.NewLine, results.Select(result => result.declaration1).Where(value => !string.IsNullOrEmpty(value)))}
{string.Join(Environment.NewLine, results.Select(result => result.declaration2).Where(value => !string.IsNullOrEmpty(value)))}
    }}

    public sealed partial class Node
    {{
        public static partial class System<{message}>
        {{
{string.Join(Environment.NewLine, results.Select(result => result.run1).Where(value => !string.IsNullOrEmpty(value)))}
{string.Join(Environment.NewLine, results.Select(result => result.run2).Where(value => !string.IsNullOrEmpty(value)))}
        }}
    }}
}}";

File.WriteAllText($"./{file}.cs", code);