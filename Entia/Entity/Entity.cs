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
using System.Runtime.CompilerServices;

namespace Entia
{
    /// <summary>
    /// Represents a world-unique identifier used to logically group components.
    /// </summary>
    /// <seealso cref="IQueryable" />
    /// <seealso cref="ITemplateable" />
    /// <seealso cref="IDependable" />
    [ThreadSafe]
    public readonly struct Entity : IQueryable, ITemplateable, IDependable, IEquatable<Entity>, IComparable<Entity>
    {
        sealed class Querier : Querier<Entity>
        {
            public override bool TryQuery(Segment segment, World world, out Query<Entity> query)
            {
                query = new Query<Entity>(index => segment.Entities.items[index]);
                return true;
            }
        }

        sealed class Instantiator : Instantiator<Entia.Entity>
        {
            public readonly Entities Entities;
            public Instantiator(Entities entities) { Entities = entities; }
            public override Result<Entia.Entity> Instantiate(object[] instances) => Entities.Create();
        }

        sealed class Initializer : Initializer<Entia.Entity>
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

        [Querier]
        static readonly Querier _querier = new Querier();
        [Templater]
        static readonly Templater _templater = new Templater();
        [Depender]
        static readonly IDepender _depender = Depender.From(new Read(typeof(Entity)));

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
        internal Entity(int index, uint generation)
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
