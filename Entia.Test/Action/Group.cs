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
    public sealed class GetEntityGroup : GetGroup<Entity>
    {
        public GetEntityGroup(MemberInfo member = null) : base(member) { }

        public override Property Check(World value, Model model) => base.Check(value, model)
            .And(_group.SequenceEqual(_group.Entities).Label("Group.SequenceEqual(Group.Entities)"));
    }

    public class GetGroup<T> : Action<World, Model> where T : struct, Queryables.IQueryable
    {
        protected MemberInfo _member;
        protected Querier<T> _querier;
        protected Segment[] _segments;
        protected Entity[] _entities;
        protected Group<T> _group;

        public GetGroup(MemberInfo member = null) { _member = member; }

        public override bool Pre(World value, Model model)
        {
            _querier = _member == null ? value.Queriers().Get<T>() : value.Queriers().Get<T>(_member);
            _segments = value.Components().Segments.Where(segment => _querier.TryQuery(new Context(segment, value), out _)).ToArray();
            _entities = _segments.SelectMany(segment => segment.Entities.Slice()).ToArray();
            return true;
        }
        public override void Do(World value, Model model) => _group = value.Groups().Get(_querier);
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
            .And(_group.Split(0).None().Label("Group.Split(0).None()"))
            .And(_group.Split(1).SelectMany(_ => _).SequenceEqual(_group).Label("Group.Split(1).SequenceEqual()"))
            .And(_group.Split(2).SelectMany(_ => _).SequenceEqual(_group).Label("Group.Split(2).SequenceEqual()"))
            .And(_group.Split(3).SelectMany(_ => _).SequenceEqual(_group).Label("Group.Split(3).SequenceEqual()"))
            .And(_entities.All(entity => _group.Has(entity)).Label("Group.Has()"))
            .And(_entities.All(entity => _group.TryGet(entity, out _)).Label("Group.TryGet()"));

        public override string ToString() => GetType().Format();
    }
}
