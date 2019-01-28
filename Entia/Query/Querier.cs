using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Modules;
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

    [ThreadSafe]
    public sealed class Default<T> : Querier<T> where T : struct, Queryables.IQueryable
    {
        static readonly FieldInfo[] _fields = typeof(T).GetFields(TypeUtility.Instance);

        public override bool TryQuery(Segment segment, World world, out Query<T> query)
        {
            var attribute = Querier.All(typeof(T).GetCustomAttributes(true).OfType<IQuerier>().ToArray());
            var querier = Querier.All(_fields.Select(field => world.Queriers().Get(field.FieldType)).ToArray());
            if (attribute.TryQuery(segment, world, out _) && querier.TryQuery(segment, world, out var inner))
            {
                query = new Query<T>(index =>
                {
                    var queryable = default(T);
                    var pointer = UnsafeUtility.Cast<T>.ToPointer(ref queryable);
                    inner.Fill((IntPtr)pointer, index);
                    return queryable;
                });
                return true;
            }

            query = default;
            return false;
        }
    }

    [ThreadSafe]
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
                var queries = new Query[queriers.Length];
                for (var i = 0; i < queriers.Length; i++)
                {
                    if (queriers[i].TryQuery(segment, world, out query)) queries[i] = query;
                    else return false;
                }

                query = new Query(
                    (pointer, index) =>
                    {
                        for (int i = 0; i < queries.Length; i++) pointer = queries[i].Fill(pointer, index);
                        return pointer;
                    },
                    queries.SelectMany(current => current.Types).ToArray());
                return true;
            });

        public static Querier<T> All<T>(Querier<T> querier, params IQuerier[] queriers) where T : struct, Queryables.IQueryable
        {
            if (queriers.Length == 0) return querier;

            var merged = All(queriers);
            return new Try<T>((Segment segment, World world, out Query<T> query) =>
                querier.TryQuery(segment, world, out query) && merged.TryQuery(segment, world, out _));
        }
    }
}