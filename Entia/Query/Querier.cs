using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Entia.Core;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queryables;

namespace Entia.Queriers
{
    public interface IQuerier
    {
        bool TryQuery(Segment segment, World world, out Query query);
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class QuerierAttribute : PreserveAttribute { }

    public abstract class Querier<T> : IQuerier where T : struct, Queryables.IQueryable
    {
        public abstract bool TryQuery(Segment segment, World world, out Query<T> query);

        bool IQuerier.TryQuery(Segment segment, World world, out Query query)
        {
            if (TryQuery(segment, world, out var casted))
            {
                query = casted;
                return true;
            }

            query = default;
            return false;
        }
    }

    public sealed class Default<T> : Querier<T> where T : struct, Queryables.IQueryable
    {
        public override bool TryQuery(Segment segment, World world, out Query<T> query)
        {
            query = default;
            return false;
        }
    }

    public static class Querier
    {
        sealed class Try : IQuerier
        {
            readonly TryFunc<Segment, World, Query> _try;
            public Try(TryFunc<Segment, World, Query> @try) { _try = @try; }
            public bool TryQuery(Segment segment, World world, out Query query) => _try(segment, world, out query);
        }

        sealed class Try<T> : Querier<T> where T : struct, Queryables.IQueryable
        {
            readonly TryFunc<Segment, World, Query<T>> _try;
            public Try(TryFunc<Segment, World, Query<T>> @try) { _try = @try; }
            public override bool TryQuery(Segment segment, World world, out Query<T> query) => _try(segment, world, out query);
        }

        public static IQuerier From(ICustomAttributeProvider provider) => All(provider.GetCustomAttributes(true).OfType<IQuerier>().ToArray());

        public static IQuerier All(params IQuerier[] queriers) =>
            queriers.Length == 1 ? queriers[0] :
            new Try((Segment segment, World world, out Query query) =>
            {
                var types = new List<Metadata>();
                for (var i = 0; i < queriers.Length; i++)
                {
                    if (queriers[i].TryQuery(segment, world, out query)) types.AddRange(query.Types);
                    else return false;
                }

                query = new Query(types.ToArray());
                return true;
            });

        public static Querier<T> All<T>(Querier<T> querier, params IQuerier[] queriers) where T : struct, Queryables.IQueryable
        {
            if (queriers.Length == 0) return querier;

            var merged = All(queriers);
            return new Try<T>((Segment segment, World world, out Query<T> query) =>
            {
                if (querier.TryQuery(segment, world, out var query1) && merged.TryQuery(segment, world, out var query2))
                {
                    query = new Query<T>(query1.Get, query1.Types, query2.Types);
                    return true;
                }

                query = default;
                return false;
            });
        }
    }
}