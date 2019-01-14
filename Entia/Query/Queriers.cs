using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Entia.Core;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queryables;
using Entia.Queriers;

namespace Entia.Modules
{
    public sealed class Queriers : IModule, IEnumerable<IQuerier>
    {
        readonly World _world;
        readonly TypeMap<Queryables.IQueryable, IQuerier> _defaults = new TypeMap<Queryables.IQueryable, IQuerier>();
        readonly TypeMap<Queryables.IQueryable, IQuerier> _queriers = new TypeMap<Queryables.IQueryable, IQuerier>();

        public Queriers(World world) { _world = world; }

        public bool TryQuery<T>(Segment segment, out Query<T> query) where T : struct, Queryables.IQueryable => Get<T>().TryQuery(segment, _world, out query);

        public Querier<T> Default<T>() where T : struct, Queryables.IQueryable =>
            _defaults.Default(typeof(T), typeof(Queryables.IQueryable<>), typeof(QuerierAttribute), () => new Default<T>()) as Querier<T>;
        public IQuerier Default(Type queryable) =>
            _defaults.Default(queryable, typeof(Queryables.IQueryable<>), typeof(QuerierAttribute), typeof(Default<>));

        public bool Has<T>() where T : struct, Queryables.IQueryable => _queriers.Has<T>(true);
        public bool Has(Type queryable) => _queriers.Has(queryable, true);
        public Querier<T> Get<T>() where T : struct, Queryables.IQueryable => _queriers.TryGet<T>(out var querier, true) && querier is Querier<T> casted ? casted : Default<T>();
        public IQuerier Get(Type queryable) => _queriers.TryGet(queryable, out var querier, true) ? querier : Default(queryable);
        public bool Set<T>(Querier<T> querier) where T : struct, Queryables.IQueryable => _queriers.Set<T>(querier);
        public bool Set(Type queryable, IQuerier querier) => _queriers.Set(queryable, querier);
        public bool Remove<T>() where T : struct, Queryables.IQueryable => _queriers.Remove<T>();
        public bool Clear() => _defaults.Clear() | _queriers.Clear();

        public IEnumerator<IQuerier> GetEnumerator() => _queriers.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}