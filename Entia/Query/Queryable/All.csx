IEnumerable<string> Generate(int depth)
{
    IEnumerable<string> GenericParameters(int count)
    {
        for (var i = 1; i <= count; i++)
            yield return $"T{i}";
    }

    for (var i = 2; i <= depth; i++)
    {
        var generics = GenericParameters(i).ToArray();
        var type = $"All<{string.Join(", ", generics)}>";
        var interfaces = $"IQueryable, IDepend<{string.Join(", ", generics)}>";
        var constraints = string.Join(" ", generics.Select(generic => $"where {generic} : struct, IQueryable"));
        var fields = string.Join(Environment.NewLine, generics.Select((generic, index) => $"public readonly {generic} Value{index + 1};"));
        var inValues = string.Join(", ", generics.Select((generic, index) => $"in {generic} value{index + 1}"));
        var initializers = string.Join(Environment.NewLine, generics.Select((_, index) => $"Value{index + 1} = value{index + 1};"));
        var queryDeclarations = string.Join(
            Environment.NewLine,
            generics.Select((generic, index) => $"var query{index + 1} = world.Queriers().Query<{generic}>();"));
        var queryArguments = string.Join(", ", generics.Select((_, index) => $"query{index + 1}"));
        var queryTryGets = string.Join(
            $" &&{Environment.NewLine}					",
            generics.Select((_, index) => $"query{index + 1}.TryGet(entity, out var item{index + 1})"));
        var itemArguments = string.Join(", ", generics.Select((_, index) => $"item{index + 1}"));

        yield return
$@"public readonly struct {type} : {interfaces}
	{constraints}
{{
	sealed class Querier : Querier<{type}>
	{{
		public override Query<{type}> Query(World world)
		{{
			{queryDeclarations}
			return new Query<{type}>(
				Modules.Query.Query.All({queryArguments}),
				(Entity entity, out {type} value) =>
				{{
					if ({queryTryGets})
					{{
						value = new {type}({itemArguments});
						return true;
					}}

					value = default;
					return false;
				}});
		}}
	}}

	[Querier]
	static readonly Querier _querier = new Querier();

	{fields}

	public All({inValues})
	{{
		{initializers}
	}}
}}";
    }
}

var file = "All";
var code =
$@"/* DO NOT MODIFY: The content of this file has been generated by the script '{file}.csx'. */

using Entia.Dependables;
using Entia.Modules;
using Entia.Modules.Query;
using Entia.Queriers;

namespace Entia.Queryables
{{
    {string.Join(Environment.NewLine, Generate(9))}
}}";

File.WriteAllText($"./{file}.cs", code);