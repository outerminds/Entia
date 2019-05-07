using Entia.Core;
using Entia.Core.Documentation;
using Entia.Messages;
using Entia.Modules.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Entia.Modules
{
    /// <summary>
    /// Module that manages entities.
    /// </summary>
    public sealed class Entities : IModule, IResolvable, IEnumerable<Entities.Enumerator, Entity>
    {
        /// <summary>
        /// An enumerator that enumerates over all existing entities.
        /// </summary>
        public struct Enumerator : IEnumerator<Entity>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public Entity Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new Entity(_index, _entities._data.items[_index].Generation);
            }
            object IEnumerator.Current => Current;

            Entities _entities;
            int _index;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> struct.
            /// </summary>
            /// <param name="entities">The entities.</param>
            public Enumerator(Entities entities)
            {
                _entities = entities;
                _index = -1;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_index < _entities._data.count)
                    if (_entities._data.items[_index].Allocated) return true;

                return false;
            }
            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset() => _index = -1;
            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose() => _entities = null;
        }

        struct Data
        {
            public uint Generation;
            public bool Allocated;
            public bool Alive;
        }

        /// <summary>
        /// Gets the current entity capacity.
        /// </summary>
        /// <value>
        /// The capacity.
        /// </value>
        [ThreadSafe]
        public int Capacity => _data.items.Length;
        /// <summary>
        /// Gets the current entity count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        [ThreadSafe]
        public int Count => _data.count - _free.count - _frozen.count;

        readonly Emitter<OnCreate> _onCreate;
        readonly Emitter<OnPreDestroy> _onPreDestroy;
        readonly Emitter<OnPostDestroy> _onPostDestroy;
        (int[] items, int count) _free = (new int[8], 0);
        (int[] items, int count) _frozen = (new int[8], 0);
        (Data[] items, int count) _data = (new Data[64], 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="Entities"/> class.
        /// </summary>
        /// <param name="messages">The messages.</param>
        public Entities(Messages messages)
        {
            _onCreate = messages.Emitter<OnCreate>();
            _onPreDestroy = messages.Emitter<OnPreDestroy>();
            _onPostDestroy = messages.Emitter<OnPostDestroy>();
        }

        /// <summary>
        /// Creates a world-unique entity.
        /// </summary>
        /// <returns>The entity.</returns>
        public Entity Create()
        {
            var reserved = ReserveIndex();
            ref var data = ref _data.items[reserved];
            var entity = new Entity(reserved, ++data.Generation);
            data.Allocated = true;
            data.Alive = true;
            _onCreate.Emit(new OnCreate { Entity = entity });
            return entity;
        }

        /// <summary>
        /// Destroys an existing <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Returns <c>true</c> if the entity was destroyed; otherwise, <c>false</c>.</returns>
        public bool Destroy(Entity entity)
        {
            ref var data = ref GetData(entity, out var success);
            return success && Destroy(entity, ref data);
        }

        /// <summary>
        /// Determines whether the <paramref name="entity"/> is alive.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Returns <c>true</c> if the <paramref name="entity"/> is alive; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool Has(Entity entity)
        {
            GetData(entity, out var success);
            return success;
        }

        /// <summary>
        /// Clears all entities.
        /// </summary>
        /// <returns>Returns <c>true</c> if any entity was destroyed; otherwise, <c>false</c>.</returns>
        public bool Clear()
        {
            var cleared = false;
            for (var i = 0; i < _data.count; i++)
            {
                ref var data = ref _data.items[i];
                if (data.Allocated)
                {
                    cleared = true;
                    Destroy(new Entity(i, data.Generation), ref data);
                }
            }
            // NOTE: do not clear '_data', '_free' and '_frozen' such that the generation counters are not lost; this prevents collisions if a reference to an old entity was kept
            return cleared;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        [ThreadSafe]
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool IResolvable.Resolve()
        {
            var resolved = _frozen.count > 0;
            while (_frozen.count > 0) _free.Push(_frozen.Pop());
            return resolved;
        }

        [ThreadSafe]
        ref Data GetData(Entity entity, out bool success)
        {
            if (entity.Index < _data.count)
            {
                ref var data = ref _data.items[entity.Index];
                success = data.Allocated && data.Generation == entity.Generation;
                return ref data;
            }

            success = false;
            return ref Dummy<Data>.Value;
        }

        bool Destroy(Entity entity, ref Data data)
        {
            // NOTE: this guard is necessary in case a reaction to 'OnPreDestroy' calls 'Destroy'
            if (data.Alive)
            {
                data.Alive = false;
                _onPreDestroy.Emit(new OnPreDestroy { Entity = entity });
                data.Allocated = false;
                _frozen.Push(entity.Index);
                _onPostDestroy.Emit(new OnPostDestroy { Entity = entity });
                return true;
            }

            return false;
        }

        int ReserveIndex()
        {
            // NOTE: prioritizing the increase of the maximum index until it hits the capacity makes sure that all available indices are used
            var index = _data.count < _data.items.Length || _free.count == 0 ? _data.count++ : _free.Pop();
            _data.Ensure();
            return index;
        }
    }
}