using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Initializers;
using Entia.Instantiators;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Modules.Template;
using Entia.Queriers;
using Entia.Queryables;
using Entia.Templateables;
using Entia.Templaters;
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
    /// <seealso cref="ITemplateable" />
    /// <seealso cref="IDependable" />
    [ThreadSafe]
    [DebuggerTypeProxy(typeof(Entity.Debug))]
    public readonly struct Entity : IEquatable<Entity>, IComparable<Entity>, Queryables.IQueryable<Entity.Querier>, ITemplateable<Entity.Templater>, IDependable
    {
        sealed class Debug
        {
            readonly struct Item
            {
                [DebuggerBrowsable(DebuggerBrowsableState.Never)]
                public Entity Entity { get; }
                public World World { get; }
                public string Name => Entity.Name(World);
                public int Index => Entity.Index;
                public uint Generation => Entity.Generation;
                public long Identifier => Entity.Identifier;
                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public IComponent[] Components => World.Components().Get(Entity).ToArray();

                public Item(Entity entity, World world)
                {
                    Entity = entity;
                    World = world;
                }

                public override string ToString() => $"{{ World: {World}, Name: {Entity.Name(World)} }}";
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object Items
            {
                get
                {
                    var worlds = World.Instances(_entity, (world, state) => world.Entities().Has(state));
                    switch (worlds.Length)
                    {
                        case 0: return null;
                        case 1: return new Item(_entity, worlds[0]);
                        default: return worlds.Select(world => new Item(_entity, world)).ToArray();
                    }
                }
            }

            readonly Entity _entity;

            public Debug(Entity entity) { _entity = entity; }
        }

        sealed class Querier : Querier<Entity>
        {
            public override bool TryQuery(Segment segment, World world, out Query<Entity> query)
            {
                query = new Query<Entity>(index => segment.Entities.items[index]);
                return true;
            }
        }

        public sealed class Instantiator : Instantiator<Entia.Entity>
        {
            public readonly Entities Entities;
            public Instantiator(Entities entities) { Entities = entities; }
            public override Result<Entia.Entity> Instantiate(object[] instances) => Entities.Create();
        }

        public sealed class Initializer : Initializer<Entia.Entity>
        {
            public readonly int[] Components;
            public readonly World World;

            public Initializer(int[] components, World world)
            {
                Components = components;
                World = world;
            }

            public override Result<Unit> Initialize(Entia.Entity instance, object[] instances)
            {
                try
                {
                    var components = World.Components();
                    for (int i = 0; i < Components.Length; i++)
                    {
                        var reference = Components[i];
                        components.Set(instance, instances[reference] as IComponent);
                    }
                    return Result.Success();
                }
                catch (Exception exception) { return Result.Exception(exception); }
            }
        }

        sealed class Templater : ITemplater
        {
            public Result<(IInstantiator instantiator, IInitializer initializer)> Template(in Context context, World world)
            {
                if (context.Index == 0 && Result.Cast<Entia.Entity>(context.Value).TryValue(out var entity))
                {
                    var indices = new List<int>();
                    var templaters = world.Templaters();
                    foreach (var component in world.Components().Get(entity))
                    {
                        var result = templaters.Template(new Context(component, component.GetType(), context));
                        if (result.TryFailure(out var failure)) return failure;
                        if (result.TryValue(out var reference)) indices.Add(reference.Index);
                    }
                    return (new Instantiator(world.Entities()), new Initializer(indices.ToArray(), world));
                }

                return (new Instantiators.Constant(context.Value), new Initializers.Identity());
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
