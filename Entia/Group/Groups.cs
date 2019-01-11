using Entia.Core;
using Entia.Messages;
using Entia.Modules.Group;
using Entia.Modules.Query;
using Entia.Queryables;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Modules
{
    public sealed class Groups : IModule, IEnumerable<IGroup>
    {
        public int Count => _groups.Count;

        readonly Entities _entities;
        readonly Messages _messages;
        readonly Queriers _queriers;
        readonly Dictionary<IQuery, IGroup> _groups = new Dictionary<IQuery, IGroup>();
        readonly List<IGroup> _entityGroups = new List<IGroup>();
        List<IGroup>[] _componentGroups = new List<IGroup>[16];

        public Groups(Entities entities, Queriers queriers, Messages messages)
        {
            _entities = entities;
            _queriers = queriers;
            _messages = messages;
            _messages.React((in OnCreate message) => Update(message.Entity, _entityGroups));
            _messages.React((in OnPostDestroy message) => Remove(message.Entity, _entityGroups));
            _messages.React((in OnAdd message) => Update(message.Entity, message.Index.global));
            _messages.React((in OnRemove message) => Update(message.Entity, message.Index.global));
            _messages.React((in OnResolve message) =>
            {
                var groups = GetGroups(message.Store.Type);
                foreach (var entity in message.Store.Entities)
                    foreach (var group in groups) group.Update(entity);
            });
        }

        public bool Has(IGroup group) => Has(group.Query);
        public bool Has(IQuery query) => _groups.ContainsKey(query);

        public bool TryGet<T>(Query<T> query, out Group<T> group) where T : struct, IQueryable
        {
            if (_groups.TryGetValue(query, out var current) && current is Group<T> casted)
            {
                group = casted;
                return true;
            }

            group = default;
            return false;
        }

        public Group<T> Get<T>(Query<T> query) where T : struct, IQueryable
        {
            if (TryGet(query, out var group)) return group;

            _groups[query] = group = new Group<T>(query, _entities);
            Initialize(group);
            return group;
        }

        public bool Clear()
        {
            var cleared = _groups.Count > 0;
            foreach (var group in _groups.Values) group.Clear();
            _groups.Clear();
            _entityGroups.Clear();
            _componentGroups.Clear();
            return cleared;
        }

        public IEnumerator<IGroup> GetEnumerator() => _groups.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void Initialize(IGroup group)
        {
            if (group.Query.Filter.All.Equals(Filter.Empty.All)) _entityGroups.Add(group);
            foreach (var type in group.Query.Filter.Types) GetGroups(type).Add(group);
            foreach (var entity in _entities) group.Update(entity);
        }

        void Update(Entity entity, int index)
        {
            if (index < _componentGroups.Length && _componentGroups[index] is List<IGroup> groups)
                Update(entity, groups);
        }

        void Update(Entity entity, List<IGroup> groups)
        {
            for (var i = 0; i < groups.Count; i++) groups[i].Update(entity);
        }

        void Remove(Entity entity, List<IGroup> groups)
        {
            for (var i = 0; i < groups.Count; i++) groups[i].Remove(entity);
        }

        List<IGroup> GetGroups(Type type) => IndexUtility.TryGetIndex(type, out var index) ? GetGroups(index) : _entityGroups;

        List<IGroup> GetGroups(int index)
        {
            ArrayUtility.Ensure(ref _componentGroups, index + 1);
            return _componentGroups[index] ?? (_componentGroups[index] = new List<IGroup>());
        }
    }
}
