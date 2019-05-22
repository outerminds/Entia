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
    public readonly struct Context
    {
        public readonly Segment Segment;
        public readonly World World;
        public readonly States Include;

        public Context(Segment segment, World world, States include = States.Enabled)
        {
            Segment = segment;
            World = world;
            Include = include;
        }
    }

    public interface IQuerier
    {
        bool TryQuery(in Context context, out Query query);
    }

    [AttributeUsage(ModuleUtility.AttributeUsage)]
    public sealed class QuerierAttribute : PreserveAttribute { }

    public abstract class Querier<T> : IQuerier where T : struct, Queryables.IQueryable
    {
        public abstract bool TryQuery(in Context context, out Query<T> query);
        bool IQuerier.TryQuery(in Context context, out Query query)
        {
            if (TryQuery(context, out var casted))
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
        static readonly FieldInfo[] _fields = TypeUtility.Cache<T>.Data.InstanceFields
            .OrderBy(field => field.MetadataToken)
            .ToArray();

        public override bool TryQuery(in Context context, out Query<T> query)
        {
            var queriers = context.World.Queriers();
            var queries = new Query[_fields.Length];
            for (int i = 0; i < _fields.Length; i++)
            {
                var field = _fields[i];
                if (queriers.TryQuery(field, context, out var result)) queries[i] = result;
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
                queries.SelectMany(current => current.Types));
            return true;
        }
    }

    [ThreadSafe]
    public sealed class True : IQuerier
    {
        public bool TryQuery(in Context context, out Query query)
        {
            query = Query.Empty;
            return true;
        }
    }

    [ThreadSafe]
    public sealed class False : IQuerier
    {
        public bool TryQuery(in Context context, out Query query)
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
            readonly TryInFunc<Context, Query> _try;
            public Try(TryInFunc<Context, Query> @try) { _try = @try; }
            public bool TryQuery(in Context context, out Query query) => _try(context, out query);
        }

        sealed class Try<T> : Querier<T> where T : struct, Queryables.IQueryable
        {
            readonly TryInFunc<Context, Query<T>> _try;
            public Try(TryInFunc<Context, Query<T>> @try) { _try = @try; }
            public override bool TryQuery(in Context context, out Query<T> query) => _try(context, out query);
        }

        public static IQuerier From(ICustomAttributeProvider provider) => All(provider.GetCustomAttributes(true).OfType<IQuerier>().ToArray());

        public static IQuerier All(this IQuerier querier, params ICustomAttributeProvider[] providers) =>
            All(providers.SelectMany(provider => provider.GetCustomAttributes(true)).OfType<IQuerier>().Prepend(querier).ToArray());

        public static IQuerier All(params IQuerier[] queriers)
        {
            queriers = queriers.Except(queriers.OfType<True>()).ToArray();
            return
                queriers.Length == 0 ? new True() :
                queriers.Length == 1 ? queriers[0] :
                new Try((in Context context, out Query query) =>
                {
                    var queries = new Query[queriers.Length];
                    for (var i = 0; i < queriers.Length; i++)
                    {
                        if (queriers[i].TryQuery(context, out query)) queries[i] = query;
                        else return false;
                    }

                    query = new Query(
                        queries.Sum(current => current.Size),
                        (pointer, index) => { for (int i = 0; i < queries.Length; i++) queries[i].Fill(pointer, index); },
                        queries.SelectMany(current => current.Types).ToArray());
                    return true;
                });
        }

        public static Querier<T> All<T>(this Querier<T> querier, params ICustomAttributeProvider[] providers) where T : struct, Queryables.IQueryable =>
            querier.All(providers.SelectMany(provider => provider.GetCustomAttributes(true)).OfType<IQuerier>().ToArray());

        public static Querier<T> All<T>(this Querier<T> querier, params IQuerier[] queriers) where T : struct, Queryables.IQueryable
        {
            if (queriers.Length == 0) return querier;

            var merged = All(queriers);
            return
                merged is True ? querier :
                new Try<T>((in Context context, out Query<T> query) =>
                    querier.TryQuery(context, out query) && merged.TryQuery(context, out _));
        }

        public static Querier<T> Include<T>(this Querier<T> querier, params ICustomAttributeProvider[] providers) where T : struct, Queryables.IQueryable =>
            querier.Include(Include(providers));

        public static Querier<T> Include<T>(this Querier<T> querier, States? include) where T : struct, Queryables.IQueryable =>
            include is States state ? new Include<T>(state, querier) : querier;

        public static IQuerier Include(this IQuerier querier, params ICustomAttributeProvider[] providers) =>
            querier.Include(Include(providers));

        public static IQuerier Include(this IQuerier querier, States? include) =>
            include is States state ? new Include(state, querier) : querier;

        static States? Include(params ICustomAttributeProvider[] providers) => providers
            .SelectMany(provider => provider.GetCustomAttributes(true))
            .OfType<IncludeAttribute>()
            .Select(attribute => Core.Nullable.Value(attribute.States))
            .FirstOrDefault();
    }

    public sealed class Include<T> : Querier<T> where T : struct, Queryables.IQueryable
    {
        public readonly States States;
        public readonly Querier<T> Querier;

        public Include(States states, Querier<T> querier)
        {
            States = states;
            Querier = querier;
        }

        public override bool TryQuery(in Context context, out Query<T> query) => Querier.TryQuery(context.With(States), out query);
    }

    public sealed class Include : IQuerier
    {
        public readonly States States;
        public readonly IQuerier Querier;

        public Include(States states, IQuerier querier)
        {
            States = states;
            Querier = querier;
        }

        public bool TryQuery(in Context context, out Query query) => Querier.TryQuery(context.With(States), out query);
    }
}