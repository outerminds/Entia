using Entia.Core;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Group;
using Entia.Modules.Query;
using Entia.Queriers;
using FsCheck;
using System.Linq;
using System.Reflection;

namespace Entia.Test
{
    public class GetGroup<T> : Action<World, Model> where T : struct, Queryables.IQueryable
    {
        ICustomAttributeProvider _provider;
        Querier<T> _querier;
        Segment[] _segments;
        Entity[] _entities;
        Group<T> _group;

        public GetGroup(ICustomAttributeProvider provider = null) { _provider = provider; }

        public override bool Pre(World value, Model model)
        {
            _querier = _provider == null ? value.Queriers().Get<T>() : value.Queriers().Get<T>(_provider);
            _segments = value.Components().Segments.Where(segment => _querier.TryQuery(segment, value, out _)).ToArray();
            _entities = _segments.SelectMany(segment => segment.Entities.Enumerate()).ToArray();
            return true;
        }
        public override void Do(World value, Model model)
        {
            _group = value.Groups().Get(_querier);
            model.Groups.Add(_group);
        }
        public override Property Check(World value, Model model) =>
            (_group.Count == _entities.Length).Label("Group.Count")
            .And((_group.Count == _group.Count()).Label("Group.Count()"))
            .And((_group.ToArray().Length == _group.Count).Label("Group.ToArray()"))
            .And((value.Groups().Get(_querier) == _group).Label("Groups.Get()"))
            .And(_group.Querier.Equals(_querier).Label("Group.Query"))
            .And(_group.Entities.OrderBy(_ => _).SequenceEqual(_entities.OrderBy(_ => _)).Label("Group.Entities"))
            .And((_group.Entities.Count() == _group.Count).Label("Group.Entities.Count()"))
            .And(_group.Entities.All(_group.Has).Label("Group.Entities.Has()"))
            .And(_group.Entities.All(entity => _group.TryGet(entity, out _)).Label("Group.Entities.TryGet()"))
            .And(_entities.All(entity => _group.Has(entity)).Label("Group.Has()"))
            .And(_entities.All(entity => _group.TryGet(entity, out _)).Label("Group.TryGet()"));
        public override string ToString() => GetType().Format();
    }
}
