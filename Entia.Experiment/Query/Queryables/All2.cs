using Entia.Core;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers2;
using Entia.Queryables;

namespace Entia.Queryables2
{
    public readonly struct All2<T1, T2> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable
    {
        sealed class Querier : IQuerier<All2<T1, T2>>
        {
            public bool TryQuery(Segment segment, World world, out Query2<All2<T1, T2>> query)
            {
                if (world.Queriers2().TryQuery<T1>(segment, out var query1) &&
                    world.Queriers2().TryQuery<T2>(segment, out var query2))
                {
                    query = new Query2<All2<T1, T2>>(index => new All2<T1, T2>(query1.Get(index), query2.Get(index)), query1.Types, query2.Types);
                    return true;
                }

                query = default;
                return false;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();

        public readonly T1 Value1;
        public readonly T2 Value2;

        public All2(in T1 value1, in T2 value2)
        {
            Value1 = value1;
            Value2 = value2;
        }
    }
}
