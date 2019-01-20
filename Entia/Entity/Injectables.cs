using Entia.Core;
using Entia.Dependables;
using Entia.Injectors;
using Entia.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Injectables
{
    public readonly struct AllEntities : IInjectable, IEnumerable<Entity>,
        IDepend<Write<Entity>, Emit<Messages.OnCreate>, Emit<Messages.OnPreDestroy>, Emit<Messages.OnPostDestroy>>
    {
        public readonly struct Read : IInjectable, IEnumerable<Entity>, IDepend<Read<Entity>>
        {
            sealed class Injector : Injector<Read>
            {
                public override Result<Read> Inject(MemberInfo member, World world) => new Read(world.Entities());
            }

            [Injector]
            static readonly Injector _injector = new Injector();

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

        [Injector]
        static readonly Injector _injector = new Injector();

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
