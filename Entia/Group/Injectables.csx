using System;
using System.Collections.Generic;
using System.Linq;

IEnumerable<string> Generate(int depth)
{
    IEnumerable<string> GenericParameters(int count)
    {
        if (count == 1) yield return "T";
        else for (var i = 1; i <= count; i++) yield return $"T{i}";
    }

    for (var i = 1; i <= depth; i++)
    {
        var generics = GenericParameters(i);
        var parameters = string.Join(", ", generics);
        var itemType = i == 1 ? parameters : $"All<{string.Join(", ", generics)}>";
        var constraints = string.Join(" ", generics.Select(generic => $"where {generic} : struct, IQueryable"));

        yield return
$@"    /// <summary>
    /// Gives access to group operations.
    /// </summary>
    [ThreadSafe]
    public readonly struct Group<{parameters}> : IInjectable, IEnumerable<{itemType}> {constraints}
    {{
        [Injector]
        static Injector<object> Injector => Injectors.Injector.From<object>((member, world) => new Group<{parameters}>(world.Groups().Get(world.Queriers().Get<{itemType}>(member))));
        [Depender]
        static IDepender Depender => Dependers.Depender.From<{itemType}>(new Dependencies.Read(typeof(Entity)));

        /// <inheritdoc cref=""Modules.Group.Group{{T}}.Count""/>
        public int Count => _group.Count;
        /// <inheritdoc cref=""Modules.Group.Group{{T}}.Segments""/>
        public Segment<{itemType}>[] Segments => _group.Segments;
        /// <inheritdoc cref=""Modules.Group.Group{{T}}.Entities""/>
        public Modules.Group.Group<{itemType}>.EntityEnumerable Entities => _group.Entities;

        readonly Modules.Group.Group<{itemType}> _group;

        /// <summary>
        /// Initializes a new instance of the <see cref=""Group{{{parameters}}}""/> struct.
        /// </summary>
        /// <param name=""group"">The group.</param>
        public Group(Modules.Group.Group<{itemType}> group) {{ _group = group; }}
        /// <inheritdoc cref=""Modules.Group.Group{{T}}.Has(Entity)""/>
        public bool Has(Entity entity) => _group.Has(entity);
        /// <inheritdoc cref=""Modules.Group.Group{{T}}.TryGet(Entity, out T)""/>
        public bool TryGet(Entity entity, out {itemType} item) => _group.TryGet(entity, out item);
        /// <inheritdoc cref=""Modules.Group.Group{{T}}.Split(int)""/>
        public Modules.Group.Group<{itemType}>.SplitEnumerable Split(int count) => _group.Split(count);
        /// <inheritdoc cref=""Modules.Group.Group{{T}}.GetEnumerator""/>
        public Modules.Group.Group<{itemType}>.Enumerator GetEnumerator() => _group.GetEnumerator();
        IEnumerator<{itemType}> IEnumerable<{itemType}>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }}
";
    }
}

var file = "Injectables";
var code =
$@"/* DO NOT MODIFY: The content of this file has been generated by the script '{file}.csx'. */

using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Injectors;
using Entia.Modules;
using Entia.Modules.Group;
using Entia.Queryables;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Injectables
{{
{string.Join(Environment.NewLine, Generate(7))}
}}";

File.WriteAllText($"./{file}.cs", code);