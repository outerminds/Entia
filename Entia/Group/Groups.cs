using Entia.Core;
using Entia.Modules.Group;
using Entia.Queriers;
using Entia.Queryables;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Modules
{
    /// <summary>
    /// Module that manages groups.
    /// </summary>
    public sealed class Groups : IModule, IClearable, IEnumerable<IGroup>
    {
        /// <summary>
        /// Gets the current group count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count => _groups.Count;

        readonly World _world;
        readonly Queriers _queriers;
        readonly Dictionary<IQuerier, IGroup> _groups = new Dictionary<IQuerier, IGroup>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Groups"/> class.
        /// </summary>
        /// <param name="world">The world.</param>
        public Groups(World world)
        {
            _world = world;
            _queriers = world.Queriers();
        }

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
        /// Gets or creates a group associated with the provided query of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The query type.</typeparam>
        /// <returns>The group.</returns>
        public Group<T> Get<T>() where T : struct, IQueryable => Get(_queriers.Get<T>());

        /// <summary>
        /// Gets or creates a group associated with the provided query of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The query type.</typeparam>
        /// <param name="member">The member that holds the group.</param>
        /// <returns>The group.</returns>
        public Group<T> Get<T>(MemberInfo member) where T : struct, IQueryable => Get(_queriers.Get<T>(member));

        /// <summary>
        /// Determines whether the provided <paramref name="group"/> already exists.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns>Returns <c>true</c> if the group was found; otherwise, <c>false</c>. </returns>
        public bool Has(IGroup group) => _groups.TryGetValue(group.Querier, out var other) && group == other;

        /// <summary>
        /// Clears all existing groups.
        /// </summary>
        /// <returns>Returns <c>true</c> if at least one group was cleared; otherwise, <c>false</c>. </returns>
        public bool Clear() => _groups.TryClear();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Dictionary<IQuerier, IGroup>.ValueCollection.Enumerator GetEnumerator() => _groups.Values.GetEnumerator();
        IEnumerator<IGroup> IEnumerable<IGroup>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}