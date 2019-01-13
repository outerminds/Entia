using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers2;
using Entia.Queryables;

namespace Entia.Queryables2
{
    public readonly struct Maybe2<T> : IQueryable where T : struct, IQueryable
    {
        sealed class Querier : IQuerier<Maybe2<T>>
        {
            public bool TryQuery(Segment segment, World world, out Query2<Maybe2<T>> query)
            {
                query = world.Queriers2().TryQuery<T>(segment, out var inner) ?
                    new Query2<Maybe2<T>>(index => new Maybe2<T>(inner.Get(index)), inner.Types) :
                    new Query2<Maybe2<T>>(_ => default);
                return true;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();

        public static implicit operator Maybe2<T>(in T value) => new Maybe2<T>(value);

        public readonly T Value;
        public readonly bool Has;

        public Maybe2(in T value)
        {
            Value = value;
            Has = true;
        }
    }
}
