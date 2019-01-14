using System;
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

    public abstract class Querier<T> : IQuerier where T : struct, IQueryable
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
}