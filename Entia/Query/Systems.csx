IEnumerable<(string system, string scheduler, string depender)> Generate(int depth)
{
    IEnumerable<string> GenericParameters(int count)
    {
        if (count == 1) yield return "T";
        else for (var i = 1; i <= count; i++) yield return $"T{i}";
    }

    for (int i = 0; i <= depth; i++)
    {
        var generics = GenericParameters(i).ToArray();
        var parameters = i == 0 ? "" : $"<{string.Join(", ", generics)}>";
        var refs = i == 0 ? "" : string.Join("", generics.Select((generic, index) => $", ref {generic} component{index + 1}"));
        var constraints = i == 0 ? "" : string.Join("", generics.Select(generic => $" where {generic} : struct, IComponent"));
        var queryable =
            i == 0 ? "Entity" :
            i == 1 ? $"Write<{generics[0]}>" :
            $"All<{string.Join(", ", generics.Select(generic => $"Write<{generic}>"))}>";
        var storeVars = string.Join("", generics.Select((generic, index) => $"var store{index + 1} = segment.Store<{generic}>();"));
        var storeRefs = string.Join("", generics.Select((generic, index) => $", ref store{index + 1}[j]"));
        var yields = string.Join(
            Environment.NewLine + "            ",
            generics.Select(generic => $"foreach (var dependency in world.Dependers().Dependencies<Write<{generic}>>()) yield return dependency;"));

        yield return (
$@"    public interface IRunEach{parameters} : ISystem, ISchedulable<Schedulers.RunEach{parameters}>, IDependable<Dependers.RunEach{parameters}>{constraints}
    {{
        void Run(Entity entity{refs});
    }}",
$@"    public sealed class RunEach{parameters} : Scheduler<IRunEach{parameters}>{constraints}
    {{
        delegate void Run(Entity entity{refs});

        public override Type[] Phases {{ get; }} = new[] {{ typeof(Phases.Run) }};

        public override Phase[] Schedule(IRunEach{parameters} instance, Controller controller)
        {{
            var world = controller.World;
            var run = new Run(instance.Run);
            var box = world.Segments<{queryable}>(run.Method);
            return new[]
            {{
                Phase.From((in Phases.Run _) =>
                {{
                    var segments = box.Value.Segments;
                    for (int i = 0; i < segments.Length; i++)
                    {{
                        var segment = segments[i];
                        var (entities, count) = segment.Entities;
                        {storeVars}
                        for (int j = 0; j < count; j++) run(entities[j]{storeRefs});
                    }}
                }})
            }};
        }}
    }}",
$@"    public sealed class RunEach{parameters} : IDepender{constraints}
    {{
        public IEnumerable<IDependency> Depend(MemberInfo member, World world)
        {{
            yield return new Read(typeof(Entity));
            {yields}
        }}
    }}");
    }
}

var file = "Systems";
var results = Generate(7).ToArray();
var code =
$@"/* DO NOT MODIFY: The content of this file has been generated by the script '{file}.csx'. */

using Entia.Schedulables;
using Entia.Dependables;
using Entia.Systems;
using System;
using Entia.Modules.Schedule;
using Entia.Modules.Query;
using Entia.Queryables;
using System.Reflection;
using System.Collections.Generic;
using Entia.Dependencies;
using Entia.Modules;

namespace Entia.Systems
{{
{string.Join(Environment.NewLine, results.Select(result => result.system))}
}}

namespace Entia.Schedulers
{{
{string.Join(Environment.NewLine, results.Select(result => result.scheduler))}
}}

namespace Entia.Dependers
{{
{string.Join(Environment.NewLine, results.Select(result => result.depender))}
}}";

File.WriteAllText($"./{file}.cs", code);