using Entia.Core;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Injectors;
using Entia.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Injectables
{
    public readonly struct AllEntities : IInjectable, IEnumerable<Entity>
    {
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

            public int Count => _entities.Count;

            readonly Entities _entities;

            public Read(Entities entities) { _entities = entities; }

            public bool Has(Entity entity) => _entities.Has(entity);

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

        public int Count => _entities.Count;

        readonly Entities _entities;

        public AllEntities(Entities entities) { _entities = entities; }

        public Entity Create() => _entities.Create();
        public bool Destroy(Entity entity) => _entities.Destroy(entity);
        public bool Has(Entity entity) => _entities.Has(entity);
        public bool Clear() => _entities.Clear();

        public Entities.Enumerator GetEnumerator() => _entities.GetEnumerator();
        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
