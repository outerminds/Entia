using System;
using Entia.Core;
using Entia.Modules.Component;
using Entia.Modules.Query;

namespace Entia.Queriers2
{
    public interface IQuerier { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class QuerierAttribute : PreserveAttribute { }

    public interface IQuerier<T> : IQuerier where T : struct, Queryables.IQueryable
    {
        bool TryQuery(Segment segment, World world, out Query2<T> query);
    }

    public sealed class Default<T> : IQuerier<T> where T : struct, Queryables.IQueryable
    {
        public bool TryQuery(Segment segment, World world, out Query2<T> query)
        {
            query = default;
            return false;
        }
    }
}