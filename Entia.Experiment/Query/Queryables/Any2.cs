using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers2;
using Entia.Queryables;

namespace Entia.Queryables2
{
    public readonly struct Any2<T1, T2> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable
    {
        sealed class Querier : IQuerier<Any2<T1, T2>>
        {
            public bool TryQuery(Segment segment, World world, out Query2<Any2<T1, T2>> query)
            {
                if (world.Queriers2().TryQuery<T1>(segment, out var query1))
                {
                    query = new Query2<Any2<T1, T2>>(index => new Any2<T1, T2>(query1.Get(index)), query1.Types);
                    return true;
                }
                if (world.Queriers2().TryQuery<T2>(segment, out var query2))
                {
                    query = new Query2<Any2<T1, T2>>(index => new Any2<T1, T2>(query2.Get(index)), query2.Types);
                    return true;
                }

                query = default;
                return false;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();

        public readonly Maybe2<T1> Value1;
        public readonly Maybe2<T2> Value2;

        public Any2(in T1 value) : this() { Value1 = value; }
        public Any2(in T2 value) : this() { Value2 = value; }
    }
}
