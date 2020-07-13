using System.Collections.Generic;

IEnumerable<string> Generate(int depth)
{
    static IEnumerable<string> GenericParameters(int count)
    {
        if (count == 1) yield return "T";
        else for (var i = 1; i <= count; i++) yield return $"T{i}";
    }

    for (int i = 1; i <= depth; i++)
    {
        var generics = GenericParameters(i).ToArray();
        var constraints = generics.Select((generic, index) => $"where {generic} : IInjectable");
        var resultVars = generics.Select((generic, index) => $"var result{index + 1} = world.Inject<{generic}>();");
        var resultTries = generics.Select((_, index) => $"result{index + 1}.TryValue(out var value{index + 1})");
        var resultMessages = generics.Select((_, index) => $"result{index + 1}.Messages()");
        var arguments = generics.Select((_, index) => $"value{index + 1}");
        var dependencies = generics.Select(generic => $"world.Dependencies<{generic}>()");

        yield return
$@"public static Node Inject<{string.Join(", ", generics)}>(Func<{string.Join(", ", generics.Append("Node"))}> provide) {string.Join(" ", constraints)} => From(new Lazy(world =>
{{
    {string.Join(" ", resultVars)}
    if ({string.Join(" && ", resultTries)})
        return provide({string.Join(", ", arguments)}).Depend(new [] {{ {string.Join(", ", dependencies)} }}.Flatten());
    else
        return Result.Failure(ArrayUtility.Concatenate({string.Join(", ", resultMessages)}));
}}));";
    }
}

var file = "Node.Inject";
var code =
$@"/* DO NOT MODIFY: The content of this file has been generated by the script '{file}.csx'. */

using System;
using Entia.Core;
using Entia.Injectables;
using Entia.Injection;
using Entia.Dependency;
using Entia.Experimental.Nodes;

namespace Entia.Experimental
{{
    public sealed partial class Node
    {{
{string.Join(Environment.NewLine, Generate(9))}
    }}
}}";


File.WriteAllText($"./{file}.cs", code);