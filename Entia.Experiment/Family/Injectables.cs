using System.Collections.Generic;
using Entia.Core;
using Entia.Injectors;
using Entia.Modules;
using Entia.Modules.Family;

namespace Entia.Injectables
{
    public readonly struct Family : IInjectable
    {
        [Injector]
        static readonly Injector<Family> _injector = Injector.From(world => new Family(world.Families()));

        readonly Families _families;

        public Family(Families families) { _families = families; }

        public Entity Root(Entity entity) => _families.Root(entity);
        public IEnumerable<Entity> Ancestors(Entity child) => _families.Ancestors(child);
        public IEnumerable<Entity> Descendants(Entity parent, From from) => _families.Descendants(parent, from);
        public Entity Parent(Entity child) => _families.Parent(child);
        public Slice<Entity>.Read Children(Entity parent) => _families.Children(parent);
        public bool Adopt(Entity parent, Entity child) => _families.Adopt(parent, child);
        public bool Reject(Entity child) => _families.Reject(child);
        public bool Replace(Entity child, Entity replacement) => _families.Replace(child, replacement);
        public bool Has(Entity parent, Entity child) => _families.Has(parent, child);
        public bool Clear() => _families.Clear();
    }
}