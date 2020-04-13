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

    static string Declaration(bool hasReceive, bool hasReact, IEnumerable<string> generics, IEnumerable<string> constraints)
    {
        var suffix = "";
        if (hasReceive) suffix += "M";
        if (hasReact) suffix += "M";
        var parameters = generics.Select((generic, index) => $"ref {generic} resource{index + 1}");
        if (hasReceive && hasReact)
        {
            generics = generics.Prepend($"{message}2").Prepend($"{message}1");
            parameters = parameters.Prepend($"in {message}2 message2").Prepend($"in {message}1 message1");
            constraints = constraints.Prepend($"where {message}2 : struct, IMessage").Prepend($"where {message}1 : struct, IMessage");
        }
        else if (hasReceive || hasReact)
        {
            generics = generics.Prepend(message);
            parameters = parameters.Prepend($"in {message} message");
            constraints = constraints.Prepend($"where {message} : struct, IMessage");
        }
        return $"public delegate void Run{suffix}<{string.Join(", ", generics)}>({string.Join(", ", parameters)}) {string.Join(" ", constraints)};";
    }

    static string Body1(bool hasMessage, IEnumerable<string> generics, IEnumerable<string> constraints)
    {
        var suffix = hasMessage ? "M" : "";
        var arguments = hasMessage ? generics.Prepend(react) : generics;
        var resourceVars = generics.Select((generic, index) => $"Resource<{generic}> resource{index + 1}");
        var resourceRefs = generics.Select((generic, index) => $"ref resource{index + 1}.Value");
        if (hasMessage) resourceRefs = resourceRefs.Prepend("message");
        return
$@"public static Node Run<{string.Join(", ", generics)}>(Run{suffix}<{string.Join(", ", arguments)}> run) {string.Join(" ", constraints)} =>
    With(({string.Join(", ", resourceVars)}) => Run((in {react} message) => run({string.Join(", ", resourceRefs)})));";
    }

    static string Body2(bool hasReceive, bool hasReact, IEnumerable<string> generics, IEnumerable<string> constraints)
    {
        var suffix = "";
        if (hasReceive) suffix += "M";
        if (hasReact) suffix += "M";
        var arguments = generics;
        if (hasReceive) arguments = arguments.Prepend(receive);
        if (hasReact) arguments = arguments.Prepend(react);
        var resourceVars = generics.Select((generic, index) => $"Resource<{generic}> resource{index + 1}");
        var resourceRefs = generics.Select((generic, index) => $"ref resource{index + 1}.Value");
        if (hasReceive) resourceRefs = resourceRefs.Prepend("receive");
        if (hasReact) resourceRefs = resourceRefs.Prepend("react");
        return
$@"public static Node Run<{string.Join(", ", generics)}>(Run{suffix}<{string.Join(", ", arguments)}> run) {string.Join(" ", constraints)} =>
    With(({string.Join(", ", resourceVars)}) => Run((in {react} react, in {receive} receive) => run({string.Join(", ", resourceRefs)})));";
    }

    for (int i = 1; i <= depth; i++)
    {
        var generics = GenericParameters(i).ToArray();
        var constraints = generics.Select((generic, index) => $"where {generic} : struct, IResource");
        var parameters = generics.Select((generic, index) => $"ref resource{index + 1}");

        yield return (
            Declaration(false, false, generics, constraints),
            Body1(false, generics, constraints),
            Body2(false, false, generics, constraints));
        yield return (
            Declaration(false, true, generics, constraints),
            Body1(true, generics, constraints),
            Body2(true, false, generics, constraints));
        yield return (
            Declaration(true, true, generics, constraints),
            "",
            Body2(true, true, generics, constraints));
    }
}

var results = Generate(5).ToArray();
var file = "Node.Run";
var code =
$@"/* DO NOT MODIFY: The content of this file has been generated by the script '{file}.csx'. */

using Entia.Injectables;
using Entia.Experimental.Systems;

namespace Entia.Experimental
{{
    namespace Systems
    {{
{string.Join(Environment.NewLine, results.Select(result => result.declaration).Where(value => !string.IsNullOrEmpty(value)))}
    }}

    public sealed partial class Node
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