using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependers;
using Entia.Injectors;
using Entia.Modules;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Injectables
{
    /// <summary>
    /// Gives access to all entity operations.
    /// </summary>
    public sealed class AllEntities : IInjectable, IEnumerable<Entities.Enumerator, Entity>
    {
        /// <summary>
        /// Gives access to entity read operations.
        /// </summary>
        [ThreadSafe]
        public sealed class Read : IInjectable, IEnumerable<Entities.Enumerator, Entity>
        {
            [Implementation]
            static Injector<Read> _injector => Injector.From(context => new Read(context.World.Entities()));
            [Implementation]
            static IDepender _depender => Depender.From(new Dependencies.Read(typeof(Entity)));

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

        [Implementation]
        static Injector<AllEntities> _injector => Injector.From(context => new AllEntities(context.World.Entities()));
        [Implementation]
        static IDepender _depender => Depender.From(
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
