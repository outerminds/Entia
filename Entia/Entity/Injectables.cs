using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Injectors;
using Entia.Modules;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Injectables
{
    /// <summary>
    /// Gives access to all entity operations.
    /// </summary>
    public readonly struct AllEntities : IInjectable, IEnumerable<Entity>
    {
        /// <summary>
        /// Gives access to entity read operations.
        /// </summary>
        [ThreadSafe]
        public readonly struct Read : IInjectable, IEnumerable<Entity>
        {
            [Injector]
            static readonly Injector<Read> _injector = Injector.From(world => new Read(world.Entities()));
            [Depender]
            static readonly IDepender _depender = Depender.From(new Dependencies.Read(typeof(Entity)));

            /// <inheritdoc cref="Entities.Count"/>
            public int Count => _entities.Count;

            readonly Entities _entities;

            /// <summary>
            /// Initializes a new instance of the <see cref="Read"/> struct.
            /// </summary>
            /// <param name="entities">The entities.</param>
            public Read(Entities entities) { _entities = entities; }

            /// <inheritdoc cref="Entities.Has(Entity)"/>
            public bool Has(Entity entity) => _entities.Has(entity);

            /// <inheritdoc cref="Entities.GetEnumerator"/>
            public Entities.Enumerator GetEnumerator() => _entities.GetEnumerator();
            IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Injector]
        static readonly Injector<AllEntities> _injector = Injector.From(world => new AllEntities(world.Entities()));
        [Depender]
        static readonly IDepender _depender = Depender.From(
            new Dependencies.Write(typeof(Entity)),
            new Dependencies.Emit(typeof(Messages.OnCreate)),
            new Dependencies.Emit(typeof(Messages.OnPreDestroy)),
            new Dependencies.Emit(typeof(Messages.OnPostDestroy)));

        /// <inheritdoc cref="Entities.Count"/>
        public int Count => _entities.Count;

        readonly Entities _entities;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllEntities"/> struct.
        /// </summary>
        /// <param name="entities">The entities.</param>
        public AllEntities(Entities entities) { _entities = entities; }

        /// <inheritdoc cref="Entities.Create"/>
        public Entity Create() => _entities.Create();
        /// <inheritdoc cref="Entities.Destroy(Entity)"/>
        public bool Destroy(Entity entity) => _entities.Destroy(entity);
        /// <inheritdoc cref="Entities.Has(Entity)"/>
        [ThreadSafe]
        public bool Has(Entity entity) => _entities.Has(entity);
        /// <inheritdoc cref="Entities.Clear"/>
        public bool Clear() => _entities.Clear();

        /// <inheritdoc cref="Entities.GetEnumerator"/>
        [ThreadSafe]
        public Entities.Enumerator GetEnumerator() => _entities.GetEnumerator();
        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
