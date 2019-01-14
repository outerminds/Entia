using Entia.Modules.Group;
using Entia.Queryables;
using Entia.Core;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Reflection;
using Entia.Queriers;

namespace Entia.Modules
{
    public sealed class Groups : IModule, IResolvable, IEnumerable<IGroup>
    {
        public int Count => _groups.count;

        readonly World _world;
        readonly Queriers _queriers;
        (IGroup[] items, int count) _groups = (new IGroup[8], 0);
        readonly Dictionary<IQuerier, IGroup> _querierToGroup = new Dictionary<IQuerier, IGroup>();

        public Groups(World world)
        {
            _world = world;
            _queriers = world.Queriers();
        }

        public Group<T> Get<T>(Querier<T> querier) where T : struct, IQueryable
        {
            if (_querierToGroup.TryGetValue(querier, out var value) && value is Group<T> group) return group;
            _querierToGroup[querier] = group = new Group<T>(querier, _world);
            _groups.Push(group);
            return group;
        }

        public bool Has(IGroup group) => _groups.Contains(group);
        public bool Clear()
        {
            _querierToGroup.Clear();
            return _groups.Clear();
        }

        public void Resolve()
        {
            for (int i = 0; i < _groups.count; i++) _groups.items[i].Resolve();
        }

        public ArrayEnumerator<IGroup> GetEnumerator() => _groups.Enumerate().GetEnumerator();
        IEnumerator<IGroup> IEnumerable<IGroup>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}