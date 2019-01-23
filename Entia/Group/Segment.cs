using Entia.Core;
using Entia.Queryables;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Entia.Modules.Group
{
    /// <summary>
    /// Stores the entities and items that satisfy the query of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The query type.</typeparam>
    public readonly struct Segment<T> : IEnumerable<T> where T : struct, IQueryable
    {
        /// <summary>
        /// Gets the entity count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _segment.Entities.count;
        }

        /// <summary>
        /// Gets the selection of component types that are stored in this segment.
        /// </summary>
        /// <value>
        /// The types.
        /// </value>
        public Component.Metadata[] Types
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _segment.Types.data;
        }
        /// <inheritdoc cref="Component.Segment.Entities"/>
        public Entity[] Entities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _segment.Entities.items;
        }
        /// <summary>
        /// The items.
        /// </summary>
        public readonly T[] Items;

        readonly Component.Segment _segment;

        /// <summary>
        /// Initializes a new instance of the <see cref="Segment{T}"/> struct.
        /// </summary>
        /// <param name="segment">The segment.</param>
        /// <param name="items">The items.</param>
        public Segment(Component.Segment segment, T[] items)
        {
            _segment = segment;
            Items = items;
        }

        /// <inheritdoc cref="Component.Segment.Store{T}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TComponent[] Store<TComponent>() where TComponent : struct, IComponent => _segment.Store<TComponent>();

        /// <inheritdoc cref="Component.Segment.TryStore{T}(out T[])"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryStore<TComponent>(out TComponent[] store) where TComponent : struct, IComponent => _segment.TryStore(out store);

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Slice<T>.Read.Enumerator GetEnumerator() => new Slice<T>.Read(Items, 0, Count).GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}