using System.Collections.Generic;
using System.Reflection;
using Entia.Core.Documentation;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;

namespace Entia.Queryables
{
    [ThreadSafe]
    public readonly struct Maybe<T> : IQueryable where T : struct, IQueryable
    {
        sealed class Querier : Querier<Maybe<T>>
        {
            public override bool TryQuery(Segment segment, World world, out Query<Maybe<T>> query)
            {
                query = world.Queriers().TryQuery<T>(segment, out var inner) ?
                    new Query<Maybe<T>>(index => new Maybe<T>(inner.Get(index)), inner.Types) :
                    new Query<Maybe<T>>(_ => default);
                return true;
            }
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world) => world.Dependers().Dependencies<T>();
        }

        [Querier]
        static readonly Querier _querier = new Querier();
        [Depender]
        static readonly Depender _depender = new Depender();

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
