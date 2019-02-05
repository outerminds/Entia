using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Entia.Core;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queryables;
using Entia.Queriers;
using System.Reflection;

namespace Entia.Modules
{
    public sealed class Queriers : IModule, IEnumerable<IQuerier>
    {
        sealed class Provider : Queryables.IQueryable
        {
            [Querier]
            static readonly IQuerier _querier = Querier.All();
        }

        readonly World _world;
        readonly TypeMap<Queryables.IQueryable, IQuerier> _defaults = new TypeMap<Queryables.IQueryable, IQuerier>();
        readonly TypeMap<Queryables.IQueryable, Dictionary<ICustomAttributeProvider, IQuerier>> _queriers = new TypeMap<Queryables.IQueryable, Dictionary<ICustomAttributeProvider, IQuerier>>();

        public Queriers(World world) { _world = world; }

        public bool TryQuery<T>(Segment segment, out Query<T> query) where T : struct, Queryables.IQueryable => Get<T>().TryQuery(segment, _world, out query);
        public bool TryQuery<T>(Querier<T> querier, Segment segment, out Query<T> query) where T : struct, Queryables.IQueryable => querier.TryQuery(segment, _world, out query);
        public bool TryQuery(IQuerier querier, Segment segment, out Query.Query query) => querier.TryQuery(segment, _world, out query);

        public Querier<T> Default<T>() where T : struct, Queryables.IQueryable =>
            _defaults.Default(typeof(T), typeof(Queryables.IQueryable<>), typeof(QuerierAttribute), () => new Default<T>()) as Querier<T>;
        public IQuerier Default(Type queryable) =>
            _defaults.Default(queryable, typeof(Queryables.IQueryable<>), typeof(QuerierAttribute), typeof(Default<>));

        public bool Has<T>(ICustomAttributeProvider provider = null) where T : struct, Queryables.IQueryable => Cache<T>().ContainsKey(provider ?? typeof(T));
        public bool Has(ICustomAttributeProvider provider = null) => Has(typeof(Provider), provider);
        public bool Has(Type queryable, ICustomAttributeProvider provider = null) => Cache(queryable).ContainsKey(provider ?? queryable);

        public Querier<T> Get<T>(ICustomAttributeProvider provider = null) where T : struct, Queryables.IQueryable
        {
            var cache = Cache<T>();
            provider = provider ?? typeof(T);
            if (cache.TryGetValue(provider, out var querier) && querier is Querier<T> casted) return casted;
            cache[provider] = casted = Querier.All(Default<T>(), Querier.From(provider));
            return casted;
        }

        public IQuerier Get(ICustomAttributeProvider provider = null) => Get(typeof(Provider), provider);
        public IQuerier Get(Type queryable, ICustomAttributeProvider provider = null)
        {
            var cache = Cache(queryable);
            provider = provider ?? queryable;
            return
                cache.TryGetValue(provider, out var querier) ? querier :
                cache[provider] = Querier.All(Default(queryable), Querier.From(provider));
        }

        public bool Set<T>(ICustomAttributeProvider provider, Querier<T> querier) where T : struct, Queryables.IQueryable => Cache<T>().Set(provider, querier);
        public bool Set<T>(Querier<T> querier) where T : struct, Queryables.IQueryable => Set<T>(typeof(T), querier);
        public bool Set(Type queryable, ICustomAttributeProvider provider, IQuerier querier) => Cache(queryable).Set(provider, querier);
        public bool Set(ICustomAttributeProvider provider, IQuerier querier) => Set(typeof(Provider), provider, querier);
        public bool Set(Type queryable, IQuerier querier) => Set(queryable, queryable, querier);
        public bool Remove<T>(ICustomAttributeProvider provider) where T : struct, Queryables.IQueryable => Cache<T>().Remove(provider);
        public bool Remove<T>() where T : struct, Queryables.IQueryable => _queriers.Remove<T>();
        public bool Remove(Type queryable, ICustomAttributeProvider provider) => Cache(queryable).Remove(provider);
        public bool Remove(ICustomAttributeProvider provider) => Remove(typeof(Provider), provider);
        public bool Remove(Type queryable) => _queriers.Remove(queryable);
        public bool Clear() => _defaults.Clear() | _queriers.Clear() | _queriers.Clear();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<IQuerier> GetEnumerator() => _queriers.Values.SelectMany(cache => cache.Values).Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        Dictionary<ICustomAttributeProvider, IQuerier> Cache(Type queryable) =>
            _queriers.TryGet(queryable, out var value, true) ? value :
            _queriers[queryable] = new Dictionary<ICustomAttributeProvider, IQuerier>();

        Dictionary<ICustomAttributeProvider, IQuerier> Cache<T>() where T : struct, Queryables.IQueryable =>
            _queriers.TryGet<T>(out var value) ? value :
            _queriers[typeof(T)] = new Dictionary<ICustomAttributeProvider, IQuerier>();
    }
}