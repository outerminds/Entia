using Entia.Core;
using Entia.Modules.Query;
using Entia.Queryables;
using System;

namespace Entia.Queriers
{
    public interface IQuerier_OLD
    {
        Type Type { get; }
        Query_OLD Query(World world);
    }

    public abstract class Querier_OLD<T> : IQuerier_OLD where T : struct, IQueryable
    {
        Type IQuerier_OLD.Type => typeof(T);

        public abstract Query_OLD<T> Query(World world);
        Query_OLD IQuerier_OLD.Query(World world) => Query(world);
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class QuerierAttribute_OLD : PreserveAttribute { }

    public sealed class Default_OLD<T> : Querier_OLD<T> where T : struct, IQueryable
    {
        public override Query_OLD<T> Query(World world) => new Query_OLD<T>(
            Filter.Empty,
            _ => false,
            (Entia.Entity _, out T value) => { value = default; return false; });
    }
}
