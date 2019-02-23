using Entia.Core;
using Entia.Core.Documentation;
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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Entia
{
    /// <summary>
    /// Represents a world-unique identifier used to logically group components.
    /// </summary>
    /// <seealso cref="IQueryable" />
    [ThreadSafe]
    public readonly struct Entity : Queryables.IQueryable, ITemplateable, IEquatable<Entity>, IComparable<Entity>
    {
        sealed class Querier : Querier<Entity>
        {
            public override bool TryQuery(Segment segment, World world, out Query<Entity> query)
            {
                query = new Query<Entity>(index => segment.Entities.items[index]);
                return true;
            }
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
                var components = World.Components();
                components.Clear(instance);

                foreach (var component in Components)
                {
                    var result = Result.Cast<IComponent>(instances[component]);
                    if (result.TryValue(out var value)) components.Set(instance, value);
                    else return result;
                }

                return Result.Success();
            }
        }

        sealed class Templater : Templater<Entia.Entity>
        {
            public override Result<IInitializer> Initializer(Entia.Entity value, Context context, World world)
            {
                var result = world.Components()
                    .Get(value)
                    .Select(component => world.Templaters().Template(component, context).Map(element => element.Reference))
                    .All();
                if (result.TryFailure(out var failure)) return failure;
                if (result.TryValue(out var components)) return new Initializer(components, world);
                return Result.Failure();
            }

            public override Result<IInstantiator> Instantiator(Entia.Entity value, Context context, World world) =>
                new Factory<Entity>(() => world.Entities().Create());
        }

        /// <summary>
        /// A zero initialized entity that will always be invalid.
        /// </summary>
        public static readonly Entity Zero;

        [Querier]
        static readonly Querier _querier = new Querier();
        [Depender]
        static readonly IDepender _depender = Depender.From(new Read(typeof(Entity)));
        [Templater]
        static readonly Templater _templater = new Templater();

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
