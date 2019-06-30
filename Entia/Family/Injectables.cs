using System.Collections.Generic;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Injectors;
using Entia.Modules;
using Entia.Modules.Family;

namespace Entia.Injectables
{
    public readonly struct AllFamilies : IInjectable
    {
        [ThreadSafe]
        public readonly struct Read : IInjectable
        {
            [Injector]
            static Injector<Read> Injector => Injectors.Injector.From(world => new Read(world.Families()));

            readonly Families _families;
            public Read(Families families) { _families = families; }

            public Entity Root(Entity entity) => _families.Root(entity);
            public IEnumerable<Entity> Roots() => _families.Roots();
            public IEnumerable<Entity> Ancestors(Entity child) => _families.Ancestors(child);
            public IEnumerable<Entity> Descendants(Entity parent, From from) => _families.Descendants(parent, from);
            public Entity Parent(Entity child) => _families.Parent(child);
            public Slice<Entity>.Read Children(Entity parent) => _families.Children(parent);
            public IEnumerable<Entity> Siblings(Entity child) => _families.Siblings(child);
            public IEnumerable<Entity> Family(Entity entity, From from) => _families.Family(entity, from);
            public bool Has(Entity parent, Entity child) => _families.Has(parent, child);
        }

        [Injector]
        static Injector<AllFamilies> Injector => Injectors.Injector.From(world => new AllFamilies(world.Families()));

        readonly Families _families;
        public AllFamilies(Families families) { _families = families; }

        [ThreadSafe]
        public Entity Root(Entity entity) => _families.Root(entity);
        [ThreadSafe]
        public IEnumerable<Entity> Roots() => _families.Roots();
        [ThreadSafe]
        public IEnumerable<Entity> Ancestors(Entity child) => _families.Ancestors(child);
        [ThreadSafe]
        public IEnumerable<Entity> Descendants(Entity parent, From from) => _families.Descendants(parent, from);
        [ThreadSafe]
        public Entity Parent(Entity child) => _families.Parent(child);
        [ThreadSafe]
        public Slice<Entity>.Read Children(Entity parent) => _families.Children(parent);
        [ThreadSafe]
        public IEnumerable<Entity> Siblings(Entity child) => _families.Siblings(child);
        [ThreadSafe]
        public IEnumerable<Entity> Family(Entity entity, From from) => _families.Family(entity, from);
        [ThreadSafe]
        public bool Has(Entity parent, Entity child) => _families.Has(parent, child);
        public bool Adopt(Entity parent, Entity child) => _families.Adopt(parent, child);
        public bool Adopt(Entity parent, params Entity[] children) => _families.Adopt(parent, children);
        public bool AdoptAt(int index, Entity parent, Entity child) => _families.AdoptAt(index, parent, child);
        public bool AdoptAt(int index, Entity parent, params Entity[] children) => _families.AdoptAt(index, parent, children);
        public bool Reject(Entity child) => _families.Reject(child);
        public bool Reject(params Entity[] children) => _families.Reject(children);
        public bool RejectAt(int index, Entity parent) => _families.RejectAt(index, parent);
        public bool RejectAt(int index, int count, Entity parent) => _families.RejectAt(index, count, parent);
        public bool Replace(Entity child, Entity replacement) => _families.Replace(child, replacement);
        public bool Clear() => _families.Clear();
    }
}