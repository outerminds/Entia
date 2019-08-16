using Entia.Core;
using Entia.Core.Documentation;
using Entia.Modules;
using Entia.Modules.Query;
using Entia.Queriers;

namespace Entia.Queryables
{
    [ThreadSafe]
    public readonly struct Maybe<T> : IQueryable where T : struct, IQueryable
    {
        sealed class Querier : Querier<Maybe<T>>
        {
            public override bool TryQuery(in Context context, out Query<Maybe<T>> query)
            {
                query = context.World.Queriers().TryQuery<T>(context, out var inner) ?
                    new Query<Maybe<T>>(index => new Maybe<T>(inner.Get(index)), inner.Types) :
                    new Query<Maybe<T>>(_ => default);
                return true;
            }
        }

        [Implementation]
        static readonly Querier _querier = new Querier();

        public static implicit operator Maybe<T>(in T value) => new Maybe<T>(value);

        public readonly T Value;
        public readonly bool Has;

        public Maybe(in T value)
        {
            Value = value;
            Has = true;
        }
    }
}
