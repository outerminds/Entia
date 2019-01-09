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
	public sealed class Queriers : IModule, IEnumerable<IQuerier>
	{
		readonly World _world;
		readonly TypeMap<Queryables.IQueryable, IQuerier> _defaults = new TypeMap<Queryables.IQueryable, IQuerier>();
		readonly TypeMap<Queryables.IQueryable, IQuerier> _queriers = new TypeMap<Queryables.IQueryable, IQuerier>();
		readonly TypeMap<Queryables.IQueryable, (IQuery, Query.Query)> _queries = new TypeMap<Queryables.IQueryable, (IQuery, Query.Query)>();

		public Queriers(World world) { _world = world; }

		public Query<T> Query<T>(ICustomAttributeProvider provider) where T : struct, Queryables.IQueryable
		{
			var query = Query<T>();
			var queries = provider.GetCustomAttributes(false)
				.OfType<QueryAttribute>()
				.Select(attribute => attribute.Query)
				.Append(query)
				.ToArray();
			return new Query<T>(Modules.Query.Query.All(queries), query.TryGet);
		}

		public Query<T> Query<T>() where T : struct, Queryables.IQueryable
		{
			if (_queries.TryGet<T>(out var pair) && pair.Item1 is Query<T> query) return query;
			query = Get<T>().Query(_world);
			_queries.Set<T>((query, Get(typeof(T)).Query(_world)));
			return query;
		}

		public Query.Query Query(Type queryable)
		{
			if (_queries.TryGet(queryable, out var pair)) return pair.Item2;
			var query = Get(queryable).Query(_world);
			_queries.Set(queryable, (null, query));
			return query;
		}

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
		public bool Clear() => _defaults.Clear() | _queriers.Clear() | _queries.Clear();

		public IEnumerator<IQuerier> GetEnumerator() => _queriers.Values.Concat(_defaults.Values).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
