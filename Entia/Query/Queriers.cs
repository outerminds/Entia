using Entia.Core;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Entia.Modules
{
    public sealed class Queriers : IModule
    {
        readonly World _world;
        readonly Dictionary<MemberInfo, IQuerier> _queriers = new Dictionary<MemberInfo, IQuerier>();

        public Queriers(World world) { _world = world; }

        public bool TryQuery<T>(in Context context, out Query<T> query, States? include = null) where T : struct, Queryables.IQueryable =>
            Get<T>().TryQuery(context.With(include), out query);
        public bool TryQuery(Type queryable, in Context context, out Query.Query query, States? include = null) =>
            Get(queryable).TryQuery(context.With(include), out query);
        public bool TryQuery(FieldInfo field, in Context context, out Query.Query query, States? include = null) =>
            Get(field).TryQuery(context.With(include), out query);
        public bool TryQuery<T>(Querier<T> querier, Segment segment, out Query<T> query, States include = States.All) where T : struct, Queryables.IQueryable =>
            querier.TryQuery(new Context(segment, _world, include), out query);
        public bool TryQuery(IQuerier querier, Segment segment, out Query.Query query, States include = States.All) =>
            querier.TryQuery(new Context(segment, _world, include), out query);

        public Querier<T> Get<T>() where T : struct, Queryables.IQueryable => Get<T>(typeof(T));
        public Querier<T> Get<T>(MemberInfo member) where T : struct, Queryables.IQueryable
        {
            if (_queriers.TryGetValue(member, out var querier) && querier is Querier<T> casted) return casted;
            _queriers[member] = casted = typeof(T) == member ?
                Default<T>().All(member).Include(member) :
                Default<T>().All(typeof(T), member).Include(member, typeof(T));
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
                queryable == null ? Querier.False :
                queryable == member ?
                Default(queryable).All(member).Include(member) :
                Default(queryable).All(queryable, member).Include(member, queryable);
        }

        Querier<T> Default<T>() where T : struct, Queryables.IQueryable =>
            // NOTE: use 'typeof(Querier<T>)' to reduce generic nesting
            _world.Container.TryGet(typeof(T), typeof(Querier<T>), out var querier) && querier is Querier<T> casted ?
            casted : new Default<T>();
        IQuerier Default(Type queryable) =>
            queryable.TryAsPointer(out var pointer) ? Default(pointer) :
            _world.Container.TryGet<IQuerier>(queryable, out var querier) ? querier :
            queryable.Is<IQueryable>() ? (IQuerier)Activator.CreateInstance(typeof(Default<>).MakeGenericType(queryable)) :
            Querier.True;
    }
}