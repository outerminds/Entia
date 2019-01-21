/* DO NOT MODIFY: The content of this file has been generated by the script 'All.csx'. */

using Entia.Core;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;
using Entia.Dependables;
using Entia.Dependers;
using Entia.Dependencies;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Queryables
{
    public readonly struct All<T1, T2> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable
    {
        sealed class Querier : Querier<All<T1, T2>>
        {
            public override bool TryQuery(Segment segment, World world, out Query<All<T1, T2>> query)
            {
                if (world.Queriers().TryQuery<T1>(segment, out var query1) && world.Queriers().TryQuery<T2>(segment, out var query2))
                {
                    query = new Query<All<T1, T2>>(index => new All<T1, T2>(query1.Get(index), query2.Get(index)), query1.Types, query2.Types);
                    return true;
                }

                query = default;
                return false;
            }
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                foreach (var dependency in world.Dependers().Dependencies<T1>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T2>()) yield return dependency;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();
        [Depender]
        static readonly Depender _depender = new Depender();

        public readonly T1 Value1; public readonly T2 Value2;
        public All(in T1 value1, in T2 value2) { Value1 = value1; Value2 = value2; }
    }

    public readonly struct All<T1, T2, T3> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable
    {
        sealed class Querier : Querier<All<T1, T2, T3>>
        {
            public override bool TryQuery(Segment segment, World world, out Query<All<T1, T2, T3>> query)
            {
                if (world.Queriers().TryQuery<T1>(segment, out var query1) && world.Queriers().TryQuery<T2>(segment, out var query2) && world.Queriers().TryQuery<T3>(segment, out var query3))
                {
                    query = new Query<All<T1, T2, T3>>(index => new All<T1, T2, T3>(query1.Get(index), query2.Get(index), query3.Get(index)), query1.Types, query2.Types, query3.Types);
                    return true;
                }

                query = default;
                return false;
            }
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                foreach (var dependency in world.Dependers().Dependencies<T1>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T2>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T3>()) yield return dependency;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();
        [Depender]
        static readonly Depender _depender = new Depender();

        public readonly T1 Value1; public readonly T2 Value2; public readonly T3 Value3;
        public All(in T1 value1, in T2 value2, in T3 value3) { Value1 = value1; Value2 = value2; Value3 = value3; }
    }

    public readonly struct All<T1, T2, T3, T4> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable
    {
        sealed class Querier : Querier<All<T1, T2, T3, T4>>
        {
            public override bool TryQuery(Segment segment, World world, out Query<All<T1, T2, T3, T4>> query)
            {
                if (world.Queriers().TryQuery<T1>(segment, out var query1) && world.Queriers().TryQuery<T2>(segment, out var query2) && world.Queriers().TryQuery<T3>(segment, out var query3) && world.Queriers().TryQuery<T4>(segment, out var query4))
                {
                    query = new Query<All<T1, T2, T3, T4>>(index => new All<T1, T2, T3, T4>(query1.Get(index), query2.Get(index), query3.Get(index), query4.Get(index)), query1.Types, query2.Types, query3.Types, query4.Types);
                    return true;
                }

                query = default;
                return false;
            }
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                foreach (var dependency in world.Dependers().Dependencies<T1>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T2>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T3>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T4>()) yield return dependency;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();
        [Depender]
        static readonly Depender _depender = new Depender();

        public readonly T1 Value1; public readonly T2 Value2; public readonly T3 Value3; public readonly T4 Value4;
        public All(in T1 value1, in T2 value2, in T3 value3, in T4 value4) { Value1 = value1; Value2 = value2; Value3 = value3; Value4 = value4; }
    }

    public readonly struct All<T1, T2, T3, T4, T5> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable where T5 : struct, IQueryable
    {
        sealed class Querier : Querier<All<T1, T2, T3, T4, T5>>
        {
            public override bool TryQuery(Segment segment, World world, out Query<All<T1, T2, T3, T4, T5>> query)
            {
                if (world.Queriers().TryQuery<T1>(segment, out var query1) && world.Queriers().TryQuery<T2>(segment, out var query2) && world.Queriers().TryQuery<T3>(segment, out var query3) && world.Queriers().TryQuery<T4>(segment, out var query4) && world.Queriers().TryQuery<T5>(segment, out var query5))
                {
                    query = new Query<All<T1, T2, T3, T4, T5>>(index => new All<T1, T2, T3, T4, T5>(query1.Get(index), query2.Get(index), query3.Get(index), query4.Get(index), query5.Get(index)), query1.Types, query2.Types, query3.Types, query4.Types, query5.Types);
                    return true;
                }

                query = default;
                return false;
            }
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                foreach (var dependency in world.Dependers().Dependencies<T1>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T2>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T3>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T4>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T5>()) yield return dependency;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();
        [Depender]
        static readonly Depender _depender = new Depender();

        public readonly T1 Value1; public readonly T2 Value2; public readonly T3 Value3; public readonly T4 Value4; public readonly T5 Value5;
        public All(in T1 value1, in T2 value2, in T3 value3, in T4 value4, in T5 value5) { Value1 = value1; Value2 = value2; Value3 = value3; Value4 = value4; Value5 = value5; }
    }

    public readonly struct All<T1, T2, T3, T4, T5, T6> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable where T5 : struct, IQueryable where T6 : struct, IQueryable
    {
        sealed class Querier : Querier<All<T1, T2, T3, T4, T5, T6>>
        {
            public override bool TryQuery(Segment segment, World world, out Query<All<T1, T2, T3, T4, T5, T6>> query)
            {
                if (world.Queriers().TryQuery<T1>(segment, out var query1) && world.Queriers().TryQuery<T2>(segment, out var query2) && world.Queriers().TryQuery<T3>(segment, out var query3) && world.Queriers().TryQuery<T4>(segment, out var query4) && world.Queriers().TryQuery<T5>(segment, out var query5) && world.Queriers().TryQuery<T6>(segment, out var query6))
                {
                    query = new Query<All<T1, T2, T3, T4, T5, T6>>(index => new All<T1, T2, T3, T4, T5, T6>(query1.Get(index), query2.Get(index), query3.Get(index), query4.Get(index), query5.Get(index), query6.Get(index)), query1.Types, query2.Types, query3.Types, query4.Types, query5.Types, query6.Types);
                    return true;
                }

                query = default;
                return false;
            }
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                foreach (var dependency in world.Dependers().Dependencies<T1>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T2>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T3>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T4>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T5>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T6>()) yield return dependency;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();
        [Depender]
        static readonly Depender _depender = new Depender();

        public readonly T1 Value1; public readonly T2 Value2; public readonly T3 Value3; public readonly T4 Value4; public readonly T5 Value5; public readonly T6 Value6;
        public All(in T1 value1, in T2 value2, in T3 value3, in T4 value4, in T5 value5, in T6 value6) { Value1 = value1; Value2 = value2; Value3 = value3; Value4 = value4; Value5 = value5; Value6 = value6; }
    }

    public readonly struct All<T1, T2, T3, T4, T5, T6, T7> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable where T5 : struct, IQueryable where T6 : struct, IQueryable where T7 : struct, IQueryable
    {
        sealed class Querier : Querier<All<T1, T2, T3, T4, T5, T6, T7>>
        {
            public override bool TryQuery(Segment segment, World world, out Query<All<T1, T2, T3, T4, T5, T6, T7>> query)
            {
                if (world.Queriers().TryQuery<T1>(segment, out var query1) && world.Queriers().TryQuery<T2>(segment, out var query2) && world.Queriers().TryQuery<T3>(segment, out var query3) && world.Queriers().TryQuery<T4>(segment, out var query4) && world.Queriers().TryQuery<T5>(segment, out var query5) && world.Queriers().TryQuery<T6>(segment, out var query6) && world.Queriers().TryQuery<T7>(segment, out var query7))
                {
                    query = new Query<All<T1, T2, T3, T4, T5, T6, T7>>(index => new All<T1, T2, T3, T4, T5, T6, T7>(query1.Get(index), query2.Get(index), query3.Get(index), query4.Get(index), query5.Get(index), query6.Get(index), query7.Get(index)), query1.Types, query2.Types, query3.Types, query4.Types, query5.Types, query6.Types, query7.Types);
                    return true;
                }

                query = default;
                return false;
            }
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                foreach (var dependency in world.Dependers().Dependencies<T1>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T2>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T3>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T4>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T5>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T6>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T7>()) yield return dependency;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();
        [Depender]
        static readonly Depender _depender = new Depender();

        public readonly T1 Value1; public readonly T2 Value2; public readonly T3 Value3; public readonly T4 Value4; public readonly T5 Value5; public readonly T6 Value6; public readonly T7 Value7;
        public All(in T1 value1, in T2 value2, in T3 value3, in T4 value4, in T5 value5, in T6 value6, in T7 value7) { Value1 = value1; Value2 = value2; Value3 = value3; Value4 = value4; Value5 = value5; Value6 = value6; Value7 = value7; }
    }

    public readonly struct All<T1, T2, T3, T4, T5, T6, T7, T8> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable where T5 : struct, IQueryable where T6 : struct, IQueryable where T7 : struct, IQueryable where T8 : struct, IQueryable
    {
        sealed class Querier : Querier<All<T1, T2, T3, T4, T5, T6, T7, T8>>
        {
            public override bool TryQuery(Segment segment, World world, out Query<All<T1, T2, T3, T4, T5, T6, T7, T8>> query)
            {
                if (world.Queriers().TryQuery<T1>(segment, out var query1) && world.Queriers().TryQuery<T2>(segment, out var query2) && world.Queriers().TryQuery<T3>(segment, out var query3) && world.Queriers().TryQuery<T4>(segment, out var query4) && world.Queriers().TryQuery<T5>(segment, out var query5) && world.Queriers().TryQuery<T6>(segment, out var query6) && world.Queriers().TryQuery<T7>(segment, out var query7) && world.Queriers().TryQuery<T8>(segment, out var query8))
                {
                    query = new Query<All<T1, T2, T3, T4, T5, T6, T7, T8>>(index => new All<T1, T2, T3, T4, T5, T6, T7, T8>(query1.Get(index), query2.Get(index), query3.Get(index), query4.Get(index), query5.Get(index), query6.Get(index), query7.Get(index), query8.Get(index)), query1.Types, query2.Types, query3.Types, query4.Types, query5.Types, query6.Types, query7.Types, query8.Types);
                    return true;
                }

                query = default;
                return false;
            }
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                foreach (var dependency in world.Dependers().Dependencies<T1>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T2>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T3>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T4>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T5>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T6>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T7>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T8>()) yield return dependency;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();
        [Depender]
        static readonly Depender _depender = new Depender();

        public readonly T1 Value1; public readonly T2 Value2; public readonly T3 Value3; public readonly T4 Value4; public readonly T5 Value5; public readonly T6 Value6; public readonly T7 Value7; public readonly T8 Value8;
        public All(in T1 value1, in T2 value2, in T3 value3, in T4 value4, in T5 value5, in T6 value6, in T7 value7, in T8 value8) { Value1 = value1; Value2 = value2; Value3 = value3; Value4 = value4; Value5 = value5; Value6 = value6; Value7 = value7; Value8 = value8; }
    }

    public readonly struct All<T1, T2, T3, T4, T5, T6, T7, T8, T9> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable where T5 : struct, IQueryable where T6 : struct, IQueryable where T7 : struct, IQueryable where T8 : struct, IQueryable where T9 : struct, IQueryable
    {
        sealed class Querier : Querier<All<T1, T2, T3, T4, T5, T6, T7, T8, T9>>
        {
            public override bool TryQuery(Segment segment, World world, out Query<All<T1, T2, T3, T4, T5, T6, T7, T8, T9>> query)
            {
                if (world.Queriers().TryQuery<T1>(segment, out var query1) && world.Queriers().TryQuery<T2>(segment, out var query2) && world.Queriers().TryQuery<T3>(segment, out var query3) && world.Queriers().TryQuery<T4>(segment, out var query4) && world.Queriers().TryQuery<T5>(segment, out var query5) && world.Queriers().TryQuery<T6>(segment, out var query6) && world.Queriers().TryQuery<T7>(segment, out var query7) && world.Queriers().TryQuery<T8>(segment, out var query8) && world.Queriers().TryQuery<T9>(segment, out var query9))
                {
                    query = new Query<All<T1, T2, T3, T4, T5, T6, T7, T8, T9>>(index => new All<T1, T2, T3, T4, T5, T6, T7, T8, T9>(query1.Get(index), query2.Get(index), query3.Get(index), query4.Get(index), query5.Get(index), query6.Get(index), query7.Get(index), query8.Get(index), query9.Get(index)), query1.Types, query2.Types, query3.Types, query4.Types, query5.Types, query6.Types, query7.Types, query8.Types, query9.Types);
                    return true;
                }

                query = default;
                return false;
            }
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                foreach (var dependency in world.Dependers().Dependencies<T1>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T2>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T3>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T4>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T5>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T6>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T7>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T8>()) yield return dependency;
                foreach (var dependency in world.Dependers().Dependencies<T9>()) yield return dependency;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();
        [Depender]
        static readonly Depender _depender = new Depender();

        public readonly T1 Value1; public readonly T2 Value2; public readonly T3 Value3; public readonly T4 Value4; public readonly T5 Value5; public readonly T6 Value6; public readonly T7 Value7; public readonly T8 Value8; public readonly T9 Value9;
        public All(in T1 value1, in T2 value2, in T3 value3, in T4 value4, in T5 value5, in T6 value6, in T7 value7, in T8 value8, in T9 value9) { Value1 = value1; Value2 = value2; Value3 = value3; Value4 = value4; Value5 = value5; Value6 = value6; Value7 = value7; Value8 = value8; Value9 = value9; }
    }
}