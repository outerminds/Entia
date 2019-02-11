using Entia.Core;
using Entia.Modules.Group;
using Entia.Queriers;
using Entia.Queryables;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Modules
{
    /// <summary>
    /// Module that manages groups.
    /// </summary>
    public sealed class Groups : IModule, IEnumerable<IGroup>
    {
        /// <summary>
        /// Gets the current group count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count => _groups.Count;

        readonly World _world;
        readonly Dictionary<IQuerier, IGroup> _groups = new Dictionary<IQuerier, IGroup>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Groups"/> class.
        /// </summary>
        /// <param name="world">The world.</param>
        public Groups(World world) { _world = world; }

        /// <summary>
        /// Gets or creates a group associated with the provided <paramref name="querier"/>.
        /// </summary>
        /// <typeparam name="T">The query type.</typeparam>
        /// <param name="querier">The querier.</param>
        /// <returns>The group.</returns>
        public Group<T> Get<T>(Querier<T> querier) where T : struct, IQueryable
        {
            if (_groups.TryGetValue(querier, out var value) && value is Group<T> group) return group;
            _groups[querier] = group = new Group<T>(querier, _world);
            return group;
        }

        /// <summary>
        /// Determines whether the provided <paramref name="group"/> already exists.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns>
        ///   <c>true</c> if [has] [the specified group]; otherwise, <c>false</c>.
        /// </returns>
        public bool Has(IGroup group) => _groups.TryGetValue(group.Querier, out var other) && group == other;

        /// <summary>
        /// Clears all existing groups.
        /// </summary>
        /// <returns></returns>
        public bool Clear() => _groups.TryClear();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Dictionary<IQuerier, IGroup>.ValueCollection.Enumerator GetEnumerator() => _groups.Values.GetEnumerator();
        IEnumerator<IGroup> IEnumerable<IGroup>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}