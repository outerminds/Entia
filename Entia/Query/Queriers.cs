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
        readonly World _world;
        readonly TypeMap<Queryables.IQueryable, IQuerier> _defaults = new TypeMap<Queryables.IQueryable, IQuerier>();
        readonly Dictionary<MemberInfo, IQuerier> _queriers = new Dictionary<MemberInfo, IQuerier>();

        public Queriers(World world) { _world = world; }

        public bool TryQuery<T>(in Context context, out Query<T> query, States? include = null) where T : struct, Queryables.IQueryable =>
            Get<T>().TryQuery(context.With(include), out query);
        public bool TryQuery(Type queryable, in Context context, out Query.Query query, States? include = null) =>
            Get(queryable).TryQuery(context.With(include), out query);
        public bool TryQuery<T>(Querier<T> querier, Segment segment, out Query<T> query, States include = States.All) where T : struct, Queryables.IQueryable =>
            querier.TryQuery(new Context(segment, _world, include), out query);
        public bool TryQuery(IQuerier querier, Segment segment, out Query.Query query, States include = States.All) =>
            querier.TryQuery(new Context(segment, _world, include), out query);

        public Querier<T> Default<T>() where T : struct, Queryables.IQueryable =>
            _defaults.Default(typeof(T), typeof(Queryables.IQueryable<>), typeof(QuerierAttribute), _ => new Default<T>()) as Querier<T>;
        public IQuerier Default(Type queryable) =>
            _defaults.Default(queryable, typeof(Queryables.IQueryable<>), typeof(QuerierAttribute), typeof(Default<>));

        public bool Has<T>() where T : struct, Queryables.IQueryable => Has(typeof(T));
        public bool Has(Type queryable) => _queriers.ContainsKey(queryable);

        public Querier<T> Get<T>() where T : struct, Queryables.IQueryable
        {
            if (_queriers.TryGetValue(typeof(T), out var querier) && querier is Querier<T> casted) return casted;
            _queriers[typeof(T)] = casted = Default<T>().All(typeof(T)).Include(typeof(T));
            return casted;
        }

        public Querier<T> Get<T>(MemberInfo member) where T : struct, Queryables.IQueryable
        {
            if (_queriers.TryGetValue(member, out var querier) && querier is Querier<T> casted) return casted;
            _queriers[member] = casted = Default<T>().All(typeof(T), member).Include(member, typeof(T));
            return casted;
        }

        public IQuerier Get(Type queryable)
        {
            if (_queriers.TryGetValue(queryable, out var querier)) return querier;
            return _queriers[queryable] = Default(queryable).All(queryable).Include(queryable);
        }

        public IQuerier Get(MemberInfo member)
        {
            if (_queriers.TryGetValue(member, out var querier)) return querier;
            var queryable =
                (member as Type) ??
                (member as FieldInfo)?.FieldType ??
                (member as PropertyInfo)?.PropertyType ??
                (member as MethodInfo)?.ReturnType;
            return _queriers[member] =
                queryable == null ? new False() :
                Default(queryable).All(queryable, member).Include(member, queryable);
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