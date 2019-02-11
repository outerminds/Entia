using Entia.Core;
using Entia.Modules.Component;
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
        sealed class Provider : Queryables.IQueryable
        {
            [Querier]
            static readonly IQuerier _querier = Querier.All();
        }

        readonly World _world;
        readonly TypeMap<Queryables.IQueryable, IQuerier> _defaults = new TypeMap<Queryables.IQueryable, IQuerier>();
        readonly Dictionary<MemberInfo, IQuerier> _queriers = new Dictionary<MemberInfo, IQuerier>();

        public Queriers(World world) { _world = world; }

        public bool TryQuery<T>(Segment segment, out Query<T> query) where T : struct, Queryables.IQueryable => Get<T>().TryQuery(segment, _world, out query);
        public bool TryQuery<T>(Querier<T> querier, Segment segment, out Query<T> query) where T : struct, Queryables.IQueryable => querier.TryQuery(segment, _world, out query);
        public bool TryQuery(IQuerier querier, Segment segment, out Query.Query query) => querier.TryQuery(segment, _world, out query);

        public Querier<T> Default<T>() where T : struct, Queryables.IQueryable =>
            _defaults.Default(typeof(T), typeof(Queryables.IQueryable<>), typeof(QuerierAttribute), () => new Default<T>()) as Querier<T>;
        public IQuerier Default(Type queryable) =>
            _defaults.Default(queryable, typeof(Queryables.IQueryable<>), typeof(QuerierAttribute), typeof(Default<>));

        public bool Has<T>() where T : struct, Queryables.IQueryable => Has(typeof(T));
        public bool Has(Type queryable) => _queriers.ContainsKey(queryable);

        public Querier<T> Get<T>() where T : struct, Queryables.IQueryable
        {
            if (_queriers.TryGetValue(typeof(T), out var querier) && querier is Querier<T> casted) return casted;
            _queriers[typeof(T)] = casted = Querier.All(Default<T>(), Querier.From(typeof(T)));
            return casted;
        }

        public Querier<T> Get<T>(MemberInfo member) where T : struct, Queryables.IQueryable
        {
            if (_queriers.TryGetValue(member, out var querier) && querier is Querier<T> casted) return casted;
            _queriers[member] = casted = Querier.All(Default<T>(), Querier.From(typeof(T)), Querier.From(member));
            return casted;
        }

        public IQuerier Get(Type queryable)
        {
            if (_queriers.TryGetValue(queryable, out var querier)) return querier;
            return _queriers[queryable] = Querier.All(Default(queryable), Querier.From(queryable));
        }

        public IQuerier Get(MemberInfo member)
        {
            if (_queriers.TryGetValue(member, out var querier)) return querier;
            var queryable =
                member is Type type ? type :
                member is FieldInfo field ? field.FieldType :
                member is PropertyInfo property ? property.PropertyType :
                member is MethodInfo method ? method.ReturnType :
                typeof(Provider);
            return _queriers[member] = Querier.All(Default(queryable), Querier.From(queryable), Querier.From(member));
        }

        public bool Set<T>(Querier<T> querier) where T : struct, Queryables.IQueryable => _queriers.Set(typeof(T), querier);
        public bool Set(Type queryable, IQuerier querier) => _queriers.Set(queryable, querier);
        public bool Remove<T>() where T : struct, Queryables.IQueryable => _queriers.Remove(typeof(T));
        public bool Remove(Type queryable) => _queriers.Remove(queryable);
        public bool Clear() => _defaults.Clear() | _queriers.TryClear();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<IQuerier> GetEnumerator() => _queriers.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}