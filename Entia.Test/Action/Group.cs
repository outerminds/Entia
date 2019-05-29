using Entia.Core;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Group;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;
using FsCheck;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Entia.Test
{
    public sealed class GetEntityGroup : GetGroup<Entity>
    {
        public GetEntityGroup(MemberInfo member = null) : base(member) { }

        public override Property Check(World value, Model model)
        {
            return base.Check(value, model).And(PropertyUtility.All(Tests()));

            IEnumerable<(bool test, string label)> Tests()
            {
                yield return (_group.SequenceEqual(_group.Entities), "Group.SequenceEqual(Entities)");
                yield return (_group.All(_group.Has), "Group.All(Group.Has)");
            }
        }
    }

    public unsafe sealed class GetPointerGroup : GetGroup<GetPointerGroup.Query>
    {
        public struct Query : Queryables.IQueryable
        {
            public Entity Entity1;
            public Test.ComponentA* PointerA1;
            public Write<Test.ComponentA> WriteA;
            public Read<Test.ComponentB> ReadB;
            public Test.ComponentB* PointerB1;
            public Entity Entity2;
            public Read<Test.ComponentA> ReadA;
            public Test.ComponentB* PointerB2;
            public Test.ComponentA* PointerA2;
            public Entity Entity3;
            public Write<Test.ComponentB> WriteB;
        }

        public GetPointerGroup(MemberInfo member = null) : base(member) { }

        public override Property Check(World value, Model model)
        {
            var pointerA1 = _group.Select(item => *item.PointerA1).ToArray();
            var pointerA2 = _group.Select(item => *item.PointerA2).ToArray();
            var pointerB1 = _group.Select(item => *item.PointerB1).ToArray();
            var pointerB2 = _group.Select(item => *item.PointerB2).ToArray();
            var readA = _group.Select(item => item.ReadA.Value).ToArray();
            var readB = _group.Select(item => item.ReadB.Value).ToArray();
            var writeA = _group.Select(item => item.WriteA.Value).ToArray();
            var writeB = _group.Select(item => item.WriteB.Value).ToArray();

            return base.Check(value, model).And(PropertyUtility.All(Tests()));

            IEnumerable<(bool test, string label)> Tests()
            {
                var entities = value.Entities();
                var components = value.Components();
                yield return (pointerA1.SequenceEqual(pointerA2), "pointerA1 == pointerA2");
                yield return (pointerA1.SequenceEqual(readA), "pointerA1 == readA");
                yield return (pointerA1.SequenceEqual(writeA), "pointerA1 == writeA");
                yield return (pointerB1.SequenceEqual(pointerB2), "pointerB1 == pointerB2");
                yield return (pointerB1.SequenceEqual(readB), "pointerB1 == readB");
                yield return (pointerB1.SequenceEqual(writeB), "pointerB1 == writeB");
                yield return (_group.Entities.SequenceEqual(_group.Select(item => item.Entity1)), "Entities == item.Entity1");
                yield return (_group.Entities.SequenceEqual(_group.Select(item => item.Entity2)), "Entities == item.Entity2");
                yield return (_group.Entities.SequenceEqual(_group.Select(item => item.Entity3)), "Entities == item.Entity3");
                yield return (_group.All(item => item.Entity1 == item.Entity2 && item.Entity1 == item.Entity3 && item.Entity2 == item.Entity3), "Entity1 == Entity2 == Entity3");
                yield return (_group.All(item => item.ReadA.State == item.WriteA.State), "ReadA.State == WriteA.State");
                yield return (_group.All(item => item.ReadB.State == item.WriteB.State), "ReadB.State == WriteB.State");
                yield return (_group.Segments.All(segment => segment.Select(item => item.ReadA.State).Same()), "segment.All(ReadA.State.Same()");
                yield return (_group.Segments.All(segment => segment.Select(item => item.ReadB.State).Same()), "segment.All(ReadB.State.Same()");
                yield return (_group.Segments.All(segment => segment.Select(item => item.WriteA.State).Same()), "segment.All(WriteA.State.Same()");
                yield return (_group.Segments.All(segment => segment.Select(item => item.WriteB.State).Same()), "segment.All(WriteB.State.Same()");
            }
        }
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
