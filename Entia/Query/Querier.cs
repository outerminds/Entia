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

    [AttributeUsage(ModuleUtility.AttributeUsage)]
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
        static readonly FieldInfo[] _fields = typeof(T).GetFields(TypeUtility.Instance)
            .OrderBy(field => field.MetadataToken)
            .ToArray();

        public override bool TryQuery(Segment segment, World world, out Query<T> query)
        {
            var attribute = Querier.All(typeof(T).GetCustomAttributes(true).OfType<IQuerier>().ToArray());
            if (attribute.TryQuery(segment, world, out var query1))
            {
                var queries = new Query[_fields.Length];
                for (int i = 0; i < _fields.Length; i++)
                {
                    var field = _fields[i];
                    var querier = world.Queriers().Get(field.FieldType);
                    if (querier.TryQuery(segment, world, out var query2)) queries[i] = query2;
                    else
                    {
                        query = default;
                        return false;
                    }
                }

                query = new Query<T>(
                    index =>
                    {
                        var queryable = default(T);
                        var pointer = UnsafeUtility.Cast<T>.ToPointer(ref queryable);
                        for (int i = 0; i < queries.Length; i++)
                        {
                            var current = queries[i];
                            current.Fill(pointer, index);
                            pointer += current.Size;
                        }
                        return queryable;
                    },
                    queries.Append(query1).SelectMany(current => current.Types));
                return true;
            }

            query = default;
            return false;
        }
    }

    [ThreadSafe]
    public sealed class Default : IQuerier
    {
        public bool TryQuery(Segment segment, World world, out Query query)
        {
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

        public static IQuerier All(params IQuerier[] queriers)
        {
            queriers = queriers.Except(queriers.OfType<Default>()).ToArray();
            return
                queriers.Length == 0 ? new Default() :
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
                        queries.Sum(current => current.Size),
                        (pointer, index) => { for (int i = 0; i < queries.Length; i++) queries[i].Fill(pointer, index); },
                        queries.SelectMany(current => current.Types).ToArray());
                    return true;
                });
        }

        public static Querier<T> All<T>(Querier<T> querier, params IQuerier[] queriers) where T : struct, Queryables.IQueryable
        {
            if (queriers.Length == 0) return querier;

            var merged = All(queriers);
            return
                merged is Default ? querier :
                new Try<T>((Segment segment, World world, out Query<T> query) =>
                    querier.TryQuery(segment, world, out query) && merged.TryQuery(segment, world, out _));
        }
    }
}