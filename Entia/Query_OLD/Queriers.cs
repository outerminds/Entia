using Entia.Core;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Entia.Modules
{
    public sealed class Queriers_OLD : IModule, IEnumerable<IQuerier_OLD>
    {
        readonly World _world;
        readonly TypeMap<Queryables.IQueryable, IQuerier_OLD> _defaults = new TypeMap<Queryables.IQueryable, IQuerier_OLD>();
        readonly TypeMap<Queryables.IQueryable, IQuerier_OLD> _queriers = new TypeMap<Queryables.IQueryable, IQuerier_OLD>();
        readonly TypeMap<Queryables.IQueryable, (IQuery_OLD, Query.Query_OLD)> _queries = new TypeMap<Queryables.IQueryable, (IQuery_OLD, Query.Query_OLD)>();

        public Queriers_OLD(World world) { _world = world; }

        public Query_OLD<T> Query<T>(ICustomAttributeProvider provider) where T : struct, Queryables.IQueryable
        {
            var query = Query<T>();
            var queries = provider.GetCustomAttributes(false)
                .OfType<QueryAttribute>()
                .Select(attribute => attribute.Queryable)
                .Append(query)
                .ToArray();
            return new Query_OLD<T>(Modules.Query.Query_OLD.All(queries), query.TryGet);
        }

        public Query_OLD<T> Query<T>() where T : struct, Queryables.IQueryable
        {
            if (_queries.TryGet<T>(out var pair) && pair.Item1 is Query_OLD<T> query) return query;
            query = Get<T>().Query(_world);
            _queries.Set<T>((query, Get(typeof(T)).Query(_world)));
            return query;
        }

        public Query.Query_OLD Query(Type queryable)
        {
            if (_queries.TryGet(queryable, out var pair)) return pair.Item2;
            var query = Get(queryable).Query(_world);
            _queries.Set(queryable, (null, query));
            return query;
        }

        public Querier_OLD<T> Default<T>() where T : struct, Queryables.IQueryable =>
            _defaults.Default(typeof(T), typeof(Queryables.IQueryable<>), typeof(QuerierAttribute_OLD), () => new Default_OLD<T>()) as Querier_OLD<T>;
        public IQuerier_OLD Default(Type queryable) =>
            _defaults.Default(queryable, typeof(Queryables.IQueryable<>), typeof(QuerierAttribute_OLD), typeof(Default_OLD<>));

        public bool Has<T>() where T : struct, Queryables.IQueryable => _queriers.Has<T>(true);
        public bool Has(Type queryable) => _queriers.Has(queryable, true);
        public Querier_OLD<T> Get<T>() where T : struct, Queryables.IQueryable => _queriers.TryGet<T>(out var querier, true) && querier is Querier_OLD<T> casted ? casted : Default<T>();
        public IQuerier_OLD Get(Type queryable) => _queriers.TryGet(queryable, out var querier, true) ? querier : Default(queryable);
        public bool Set<T>(Querier_OLD<T> querier) where T : struct, Queryables.IQueryable => _queriers.Set<T>(querier);
        public bool Set(Type queryable, IQuerier_OLD querier) => _queriers.Set(queryable, querier);
        public bool Remove<T>() where T : struct, Queryables.IQueryable => _queriers.Remove<T>();
        public bool Clear() => _defaults.Clear() | _queriers.Clear() | _queries.Clear();

        public IEnumerator<IQuerier_OLD> GetEnumerator() => _queriers.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
