using Entia.Core;
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
        public readonly struct Read : IInjectable, IEnumerable<Entity>
        {
            sealed class Injector : Injector<Read>
            {
                public override Result<Read> Inject(MemberInfo member, World world) => new Read(world.Entities());
            }

            sealed class Depender : IDepender
            {
                public IEnumerable<IDependency> Depend(MemberInfo member, World world)
                {
                    yield return new Dependencies.Read(typeof(Entity));
                }
            }

            [Injector]
            static readonly Injector _injector = new Injector();
            [Depender]
            static readonly Depender _depender = new Depender();

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

        sealed class Injector : Injector<AllEntities>
        {
            public override Result<AllEntities> Inject(MemberInfo member, World world) => new AllEntities(world.Entities());
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                yield return new Dependencies.Write(typeof(Entity));
                yield return new Dependencies.Emit(typeof(Messages.OnCreate));
                yield return new Dependencies.Emit(typeof(Messages.OnPreDestroy));
                yield return new Dependencies.Emit(typeof(Messages.OnPostDestroy));
            }
        }

        [Injector]
        static readonly Injector _injector = new Injector();
        [Depender]
        static readonly Depender _depender = new Depender();

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
        public bool Has(Entity entity) => _entities.Has(entity);
        /// <inheritdoc cref="Entities.Clear"/>
        public bool Clear() => _entities.Clear();

        /// <inheritdoc cref="Entities.GetEnumerator"/>
        public Entities.Enumerator GetEnumerator() => _entities.GetEnumerator();
        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
