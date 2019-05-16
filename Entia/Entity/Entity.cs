using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Entia
{
    /// <summary>
    /// Represents a world-unique identifier used to logically group components.
    /// </summary>
    /// <seealso cref="Queryables.IQueryable" />
    /// <seealso cref="IDependable" />
    [ThreadSafe]
    [DebuggerTypeProxy(typeof(Entity.View))]
    public readonly struct Entity : IEquatable<Entity>, IComparable<Entity>, Queryables.IQueryable<Entity.Querier>, IDependable
    {
        sealed class View
        {
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object Items
            {
                get
                {
                    var worlds = World.Instances(_entity, (world, state) => world.Entities().Has(state));
                    switch (worlds.Length)
                    {
                        case 0: return null;
                        case 1: return new EntityView(_entity, worlds[0]);
                        default: return worlds.Select(world => new EntityView(_entity, world)).ToArray();
                    }
                }
            }

            readonly Entity _entity;

            public View(Entity entity) { _entity = entity; }
        }

        sealed class EntityView
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public Entity Entity { get; }
            public World World { get; }
            public string Name => Entity.Name(World);
            public int Index => Entity.Index;
            public uint Generation => Entity.Generation;
            public long Identifier => Entity.Identifier;
            public ComponentView[] Components => World.TryGet<Modules.Components>(out var components) ?
                components.Get(Entity, States.All).Select(component => new ComponentView(component, Entity, World)).ToArray() :
                Array.Empty<ComponentView>();

            public EntityView(Entity entity, World world)
            {
                Entity = entity;
                World = world;
            }

            public override string ToString() => $"{{ World: {World}, Name: {Entity.Name(World)} }}";
        }

        sealed class ComponentView
        {
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public IComponent Component { get; }
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public Entity Entity { get; }
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public World World { get; }
            public States State => World.TryGet<Modules.Components>(out var components) ?
                components.State(Entity, Component.GetType()) : States.None;

            public ComponentView(IComponent component, Entity entity, World world)
            {
                Component = component;
                Entity = entity;
                World = world;
            }
        }

        sealed class Querier : Querier<Entity>
        {
            public override bool TryQuery(Segment segment, World world, out Query<Entity> query)
            {
                query = new Query<Entity>(index => segment.Entities.items[index]);
                return true;
            }
        }

        /// <summary>
        /// A zero initialized entity that will always be invalid.
        /// </summary>
        public static readonly Entity Zero;

        [Depender]
        static readonly IDepender _depender = Dependers.Depender.From(new Read(typeof(Entity)));

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Entity a, Entity b) => a.Equals(b);
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Entity a, Entity b) => !a.Equals(b);
        /// <summary>
        /// Implements and implicit <c>bool</c> operator.
        /// </summary>
        /// <returns>Returns <c>true</c> if the entity is valid; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(Entity entity) => !entity.Equals(Zero);

        /// <summary>
        /// The world-unique identifier.
        /// </summary>
        public long Identifier
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (long)Index | ((long)Generation << 32);
        }

        /// <summary>
        /// The index where the entity is stored within its world.
        /// </summary>
        public readonly int Index;
        /// <summary>
        /// The generation of the index.
        /// </summary>
        public readonly uint Generation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity(int index, uint generation)
        {
            Index = index;
            Generation = generation;
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Entity other)
        {
            if (Index < other.Index) return -1;
            else if (Index > other.Index) return 1;
            else if (Generation < other.Generation) return -1;
            else if (Generation > other.Generation) return 1;
            else return 0;
        }
        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Entity other) => Index == other.Index && Generation == other.Generation;
        /// <inheritdoc cref="ValueType.Equals(object)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is Entity entity && Equals(entity);
        /// <inheritdoc cref="ValueType.GetHashCode"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => Index ^ (int)Generation;
        /// <inheritdoc cref="ValueType.ToString()"/>
        public override string ToString() => $"{{ Index: {Index}, Generation: {Generation} }}";
    }
}
