using Entia.Core;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Group;
using Entia.Modules.Query;
using Entia.Queriers;
using FsCheck;
using System.Collections.Generic;
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
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool test, string label)> Tests()
            {
                yield return (_group.Count == _entities.Length, "Group.Count");
                yield return (_group.Count == _group.Count(), "Group.Count()");
                yield return (_group.ToArray().Length == _group.Count, "Group.ToArray()");
                yield return (value.Groups().Get(_querier) == _group, "Groups.Get()");
                yield return (_group.Querier.Equals(_querier), "Group.Query");
                yield return (_group.Entities.OrderBy(_ => _).SequenceEqual(_entities.OrderBy(_ => _)), "Group.Entities");
                yield return (_group.Entities.Count() == _group.Count, "Group.Entities.Count()");
                yield return (_group.Entities.All(_group.Has), "Group.Entities.Has()");
                yield return (_group.Entities.All(entity => _group.TryGet(entity, out _)), "Group.Entities.TryGet()");
                yield return (_group.Split(0).None(), "Group.Split(0).None()");
                for (int i = 1; i < 5; i++) yield return (_group.Split(i).Sum(split => split.Count()) == _group.Count, $"Group.Split({i}).Count()");
                for (int i = 1; i < 5; i++) yield return (_group.Split(i).Sum(split => split.Count) == _group.Count, $"Group.Split({i}).Count");
                yield return (_group.Segments.Sum(segment => segment.Count) == _group.Count, $"Group.Segments.Count");
                yield return (_group.Segments.Sum(segment => segment.Count()) == _group.Count, $"Group.Segments.Count");
                yield return (_entities.All(entity => _group.Has(entity)), "Group.Has()");
                yield return (_entities.All(entity => _group.TryGet(entity, out _)), "Group.TryGet()");
            }
        }

        public override string ToString() => GetType().Format();
    }
}
