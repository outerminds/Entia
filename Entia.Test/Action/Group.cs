using Entia.Core;
using Entia.Modules;
using Entia.Modules.Group;
using Entia.Modules.Query;
using FsCheck;
using System.Linq;

namespace Entia.Test
{
    public class GetGroup<T> : Action<World, Model> where T : struct, Queryables.IQueryable
    {
        readonly Query_OLD? _additionnal;
        Entity[] _entities;
        Query_OLD<T> _query;
        Group_OLD<T> _group;

        public GetGroup(Query_OLD? query = null) { _additionnal = query; }

        public override bool Pre(World value, Model model)
        {
            _query = _additionnal == null ? value.Queriers_OLD().Query<T>() : Query_OLD.All(value.Queriers_OLD().Query<T>(), _additionnal.Value);
            _entities = value.Entities().Pairs.Where(pair => _query.Fits(pair.data.Mask)).Select(pair => pair.entity).ToArray();
            return true;
        }
        public override void Do(World value, Model model)
        {
            _group = value.Groups_OLD().Get(_query);
            model.Groups.Add(_group);
        }
        public override Property Check(World value, Model model) =>
            (_group.Count == _entities.Length).Label("Group.Count")
            .And((_group.Count == _group.Count()).Label("Group.Count()"))
            .And((_group.ToArray().Length == _group.Count).Label("Group.ToArray()"))
            .And((value.Groups_OLD().Get(_query) == _group).Label("Groups.Get()"))
            .And(_group.Query.Equals(_query).Label("Group.Query"))
            .And((_query.Entities(value.Entities()).Count() == _group.Count).Label("Query.Entities().Count()"))
            .And(_query.Entities(value.Entities()).OrderBy(_ => _).SequenceEqual(_group.Entities.OrderBy(_ => _)).Label("Query.Entities() == Group.Entities"))
            .And((_query.Items(value.Entities()).Count() == _group.Count).Label("Query.Items().Count()"))
            .And(_group.Entities.OrderBy(_ => _).SequenceEqual(_entities.OrderBy(_ => _)).Label("Group.Entities"))
            .And((_group.Entities.Count() == _group.Count).Label("Group.Entities.Count()"))
            .And(_group.Entities.All(_group.Has).Label("Group.Entities.Has()"))
            .And(_group.Entities.All(_group.Fits).Label("Group.Entities.Fits()"))
            .And(_group.Entities.All(entity => _group.TryGet(entity, out _)).Label("Group.Entities.TryGet()"))
            .And(_group.Entities.All(_group.Update).Label("Group.Entities.Update()"))
            .And(_entities.All(entity => _group.Fits(entity)).Label("Group.Fits()"))
            .And(_entities.All(entity => _group.Has(entity)).Label("Group.Has()"))
            .And(_entities.All(entity => _group.TryGet(entity, out _)).Label("Group.TryGet()"))
            .And(_entities.All(entity => (_group as IGroup_OLD).Update(entity)).Label("Group.Update()"));
        public override string ToString() => GetType().Format();
    }
}
