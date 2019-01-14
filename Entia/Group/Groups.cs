using Entia.Modules.Group;
using Entia.Queryables;
using Entia.Core;
using System.Collections.Generic;
using System.Collections;
using System;

namespace Entia.Modules
{
    public sealed class Groups : IModule, IResolvable, IEnumerable<IGroup>
    {
        public int Count => _groups.count;

        readonly Components _components;
        readonly Queriers _queriers;
        readonly Messages _messages;
        (IGroup[] items, int count) _groups = (new IGroup[8], 0);

        public Groups(Components components, Queriers queriers, Messages messages)
        {
            _components = components;
            _queriers = queriers;
            _messages = messages;
        }

        public Group<T> Get<T>() where T : struct, IQueryable
        {
            var group = new Group<T>(_components, _queriers, _messages);
            _groups.Push(group);
            return group;
        }

        public bool Has(IGroup group) => _groups.Contains(group);

        public bool Clear() => _groups.Clear();

        public void Resolve()
        {
            for (int i = 0; i < _groups.count; i++) _groups.items[i].Resolve();
        }

        public ArrayEnumerator<IGroup> GetEnumerator() => _groups.Enumerate().GetEnumerator();
        IEnumerator<IGroup> IEnumerable<IGroup>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}