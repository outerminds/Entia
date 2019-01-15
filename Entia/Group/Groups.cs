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
    public sealed class Groups : IModule, IEnumerable<IGroup>
    {
        public int Count => _groups.Count;

        readonly World _world;
        readonly Dictionary<IQuerier, IGroup> _groups = new Dictionary<IQuerier, IGroup>();

        public Groups(World world) { _world = world; }

        public Group<T> Get<T>(Querier<T> querier) where T : struct, IQueryable
        {
            if (_groups.TryGetValue(querier, out var value) && value is Group<T> group) return group;
            _groups[querier] = group = new Group<T>(querier, _world);
            return group;
        }

        public bool Has(IGroup group) => _groups.TryGetValue(group.Querier, out var other) && group == other;
        public bool Clear()
        {
            var cleared = _groups.Count > 0;
            _groups.Clear();
            return cleared;
        }

        public IEnumerator<IGroup> GetEnumerator() => _groups.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}