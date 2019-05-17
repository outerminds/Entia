/* DO NOT MODIFY: The content of this file has been generated by the script 'Any.csx'. */

using Entia.Core;
using Entia.Core.Documentation;
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
    /// <summary>
    /// Query that must satisfy at least one of its sub queries.
    /// Only the first match will be kept.
    /// </summary>
    [ThreadSafe]
    public readonly struct Any<T1, T2> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable
    {
        sealed class Querier : Querier<Any<T1, T2>>
        {
            public override bool TryQuery(in Context context, out Query<Any<T1, T2>> query)
            {
                var queriers = context.World.Queriers();
                if (queriers.TryQuery<T1>(context, out var query1)) { query = new Query<Any<T1, T2>>(index => new Any<T1, T2>(query1.Get(index)), query1.Types); return true; }
                if (queriers.TryQuery<T2>(context, out var query2)) { query = new Query<Any<T1, T2>>(index => new Any<T1, T2>(query2.Get(index)), query2.Types); return true; }
                query = default;
                return false;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();

        /// <summary>
        /// The value1.
        /// </summary>
        public readonly Maybe<T1> Value1;
        /// <summary>
        /// The value2.
        /// </summary>
        public readonly Maybe<T2> Value2;
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2}"/> struct.
        /// </summary>
        public Any(in T1 value) : this() { Value1 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2}"/> struct.
        /// </summary>
        public Any(in T2 value) : this() { Value2 = value; }
    }

    /// <summary>
    /// Query that must satisfy at least one of its sub queries.
    /// Only the first match will be kept.
    /// </summary>
    [ThreadSafe]
    public readonly struct Any<T1, T2, T3> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable
    {
        sealed class Querier : Querier<Any<T1, T2, T3>>
        {
            public override bool TryQuery(in Context context, out Query<Any<T1, T2, T3>> query)
            {
                var queriers = context.World.Queriers();
                if (queriers.TryQuery<T1>(context, out var query1)) { query = new Query<Any<T1, T2, T3>>(index => new Any<T1, T2, T3>(query1.Get(index)), query1.Types); return true; }
                if (queriers.TryQuery<T2>(context, out var query2)) { query = new Query<Any<T1, T2, T3>>(index => new Any<T1, T2, T3>(query2.Get(index)), query2.Types); return true; }
                if (queriers.TryQuery<T3>(context, out var query3)) { query = new Query<Any<T1, T2, T3>>(index => new Any<T1, T2, T3>(query3.Get(index)), query3.Types); return true; }
                query = default;
                return false;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();

        /// <summary>
        /// The value1.
        /// </summary>
        public readonly Maybe<T1> Value1;
        /// <summary>
        /// The value2.
        /// </summary>
        public readonly Maybe<T2> Value2;
        /// <summary>
        /// The value3.
        /// </summary>
        public readonly Maybe<T3> Value3;
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3}"/> struct.
        /// </summary>
        public Any(in T1 value) : this() { Value1 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3}"/> struct.
        /// </summary>
        public Any(in T2 value) : this() { Value2 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3}"/> struct.
        /// </summary>
        public Any(in T3 value) : this() { Value3 = value; }
    }

    /// <summary>
    /// Query that must satisfy at least one of its sub queries.
    /// Only the first match will be kept.
    /// </summary>
    [ThreadSafe]
    public readonly struct Any<T1, T2, T3, T4> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable
    {
        sealed class Querier : Querier<Any<T1, T2, T3, T4>>
        {
            public override bool TryQuery(in Context context, out Query<Any<T1, T2, T3, T4>> query)
            {
                var queriers = context.World.Queriers();
                if (queriers.TryQuery<T1>(context, out var query1)) { query = new Query<Any<T1, T2, T3, T4>>(index => new Any<T1, T2, T3, T4>(query1.Get(index)), query1.Types); return true; }
                if (queriers.TryQuery<T2>(context, out var query2)) { query = new Query<Any<T1, T2, T3, T4>>(index => new Any<T1, T2, T3, T4>(query2.Get(index)), query2.Types); return true; }
                if (queriers.TryQuery<T3>(context, out var query3)) { query = new Query<Any<T1, T2, T3, T4>>(index => new Any<T1, T2, T3, T4>(query3.Get(index)), query3.Types); return true; }
                if (queriers.TryQuery<T4>(context, out var query4)) { query = new Query<Any<T1, T2, T3, T4>>(index => new Any<T1, T2, T3, T4>(query4.Get(index)), query4.Types); return true; }
                query = default;
                return false;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();

        /// <summary>
        /// The value1.
        /// </summary>
        public readonly Maybe<T1> Value1;
        /// <summary>
        /// The value2.
        /// </summary>
        public readonly Maybe<T2> Value2;
        /// <summary>
        /// The value3.
        /// </summary>
        public readonly Maybe<T3> Value3;
        /// <summary>
        /// The value4.
        /// </summary>
        public readonly Maybe<T4> Value4;
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4}"/> struct.
        /// </summary>
        public Any(in T1 value) : this() { Value1 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4}"/> struct.
        /// </summary>
        public Any(in T2 value) : this() { Value2 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4}"/> struct.
        /// </summary>
        public Any(in T3 value) : this() { Value3 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4}"/> struct.
        /// </summary>
        public Any(in T4 value) : this() { Value4 = value; }
    }

    /// <summary>
    /// Query that must satisfy at least one of its sub queries.
    /// Only the first match will be kept.
    /// </summary>
    [ThreadSafe]
    public readonly struct Any<T1, T2, T3, T4, T5> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable where T5 : struct, IQueryable
    {
        sealed class Querier : Querier<Any<T1, T2, T3, T4, T5>>
        {
            public override bool TryQuery(in Context context, out Query<Any<T1, T2, T3, T4, T5>> query)
            {
                var queriers = context.World.Queriers();
                if (queriers.TryQuery<T1>(context, out var query1)) { query = new Query<Any<T1, T2, T3, T4, T5>>(index => new Any<T1, T2, T3, T4, T5>(query1.Get(index)), query1.Types); return true; }
                if (queriers.TryQuery<T2>(context, out var query2)) { query = new Query<Any<T1, T2, T3, T4, T5>>(index => new Any<T1, T2, T3, T4, T5>(query2.Get(index)), query2.Types); return true; }
                if (queriers.TryQuery<T3>(context, out var query3)) { query = new Query<Any<T1, T2, T3, T4, T5>>(index => new Any<T1, T2, T3, T4, T5>(query3.Get(index)), query3.Types); return true; }
                if (queriers.TryQuery<T4>(context, out var query4)) { query = new Query<Any<T1, T2, T3, T4, T5>>(index => new Any<T1, T2, T3, T4, T5>(query4.Get(index)), query4.Types); return true; }
                if (queriers.TryQuery<T5>(context, out var query5)) { query = new Query<Any<T1, T2, T3, T4, T5>>(index => new Any<T1, T2, T3, T4, T5>(query5.Get(index)), query5.Types); return true; }
                query = default;
                return false;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();

        /// <summary>
        /// The value1.
        /// </summary>
        public readonly Maybe<T1> Value1;
        /// <summary>
        /// The value2.
        /// </summary>
        public readonly Maybe<T2> Value2;
        /// <summary>
        /// The value3.
        /// </summary>
        public readonly Maybe<T3> Value3;
        /// <summary>
        /// The value4.
        /// </summary>
        public readonly Maybe<T4> Value4;
        /// <summary>
        /// The value5.
        /// </summary>
        public readonly Maybe<T5> Value5;
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5}"/> struct.
        /// </summary>
        public Any(in T1 value) : this() { Value1 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5}"/> struct.
        /// </summary>
        public Any(in T2 value) : this() { Value2 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5}"/> struct.
        /// </summary>
        public Any(in T3 value) : this() { Value3 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5}"/> struct.
        /// </summary>
        public Any(in T4 value) : this() { Value4 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5}"/> struct.
        /// </summary>
        public Any(in T5 value) : this() { Value5 = value; }
    }

    /// <summary>
    /// Query that must satisfy at least one of its sub queries.
    /// Only the first match will be kept.
    /// </summary>
    [ThreadSafe]
    public readonly struct Any<T1, T2, T3, T4, T5, T6> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable where T5 : struct, IQueryable where T6 : struct, IQueryable
    {
        sealed class Querier : Querier<Any<T1, T2, T3, T4, T5, T6>>
        {
            public override bool TryQuery(in Context context, out Query<Any<T1, T2, T3, T4, T5, T6>> query)
            {
                var queriers = context.World.Queriers();
                if (queriers.TryQuery<T1>(context, out var query1)) { query = new Query<Any<T1, T2, T3, T4, T5, T6>>(index => new Any<T1, T2, T3, T4, T5, T6>(query1.Get(index)), query1.Types); return true; }
                if (queriers.TryQuery<T2>(context, out var query2)) { query = new Query<Any<T1, T2, T3, T4, T5, T6>>(index => new Any<T1, T2, T3, T4, T5, T6>(query2.Get(index)), query2.Types); return true; }
                if (queriers.TryQuery<T3>(context, out var query3)) { query = new Query<Any<T1, T2, T3, T4, T5, T6>>(index => new Any<T1, T2, T3, T4, T5, T6>(query3.Get(index)), query3.Types); return true; }
                if (queriers.TryQuery<T4>(context, out var query4)) { query = new Query<Any<T1, T2, T3, T4, T5, T6>>(index => new Any<T1, T2, T3, T4, T5, T6>(query4.Get(index)), query4.Types); return true; }
                if (queriers.TryQuery<T5>(context, out var query5)) { query = new Query<Any<T1, T2, T3, T4, T5, T6>>(index => new Any<T1, T2, T3, T4, T5, T6>(query5.Get(index)), query5.Types); return true; }
                if (queriers.TryQuery<T6>(context, out var query6)) { query = new Query<Any<T1, T2, T3, T4, T5, T6>>(index => new Any<T1, T2, T3, T4, T5, T6>(query6.Get(index)), query6.Types); return true; }
                query = default;
                return false;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();

        /// <summary>
        /// The value1.
        /// </summary>
        public readonly Maybe<T1> Value1;
        /// <summary>
        /// The value2.
        /// </summary>
        public readonly Maybe<T2> Value2;
        /// <summary>
        /// The value3.
        /// </summary>
        public readonly Maybe<T3> Value3;
        /// <summary>
        /// The value4.
        /// </summary>
        public readonly Maybe<T4> Value4;
        /// <summary>
        /// The value5.
        /// </summary>
        public readonly Maybe<T5> Value5;
        /// <summary>
        /// The value6.
        /// </summary>
        public readonly Maybe<T6> Value6;
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5, T6}"/> struct.
        /// </summary>
        public Any(in T1 value) : this() { Value1 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5, T6}"/> struct.
        /// </summary>
        public Any(in T2 value) : this() { Value2 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5, T6}"/> struct.
        /// </summary>
        public Any(in T3 value) : this() { Value3 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5, T6}"/> struct.
        /// </summary>
        public Any(in T4 value) : this() { Value4 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5, T6}"/> struct.
        /// </summary>
        public Any(in T5 value) : this() { Value5 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5, T6}"/> struct.
        /// </summary>
        public Any(in T6 value) : this() { Value6 = value; }
    }

    /// <summary>
    /// Query that must satisfy at least one of its sub queries.
    /// Only the first match will be kept.
    /// </summary>
    [ThreadSafe]
    public readonly struct Any<T1, T2, T3, T4, T5, T6, T7> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable where T5 : struct, IQueryable where T6 : struct, IQueryable where T7 : struct, IQueryable
    {
        sealed class Querier : Querier<Any<T1, T2, T3, T4, T5, T6, T7>>
        {
            public override bool TryQuery(in Context context, out Query<Any<T1, T2, T3, T4, T5, T6, T7>> query)
            {
                var queriers = context.World.Queriers();
                if (queriers.TryQuery<T1>(context, out var query1)) { query = new Query<Any<T1, T2, T3, T4, T5, T6, T7>>(index => new Any<T1, T2, T3, T4, T5, T6, T7>(query1.Get(index)), query1.Types); return true; }
                if (queriers.TryQuery<T2>(context, out var query2)) { query = new Query<Any<T1, T2, T3, T4, T5, T6, T7>>(index => new Any<T1, T2, T3, T4, T5, T6, T7>(query2.Get(index)), query2.Types); return true; }
                if (queriers.TryQuery<T3>(context, out var query3)) { query = new Query<Any<T1, T2, T3, T4, T5, T6, T7>>(index => new Any<T1, T2, T3, T4, T5, T6, T7>(query3.Get(index)), query3.Types); return true; }
                if (queriers.TryQuery<T4>(context, out var query4)) { query = new Query<Any<T1, T2, T3, T4, T5, T6, T7>>(index => new Any<T1, T2, T3, T4, T5, T6, T7>(query4.Get(index)), query4.Types); return true; }
                if (queriers.TryQuery<T5>(context, out var query5)) { query = new Query<Any<T1, T2, T3, T4, T5, T6, T7>>(index => new Any<T1, T2, T3, T4, T5, T6, T7>(query5.Get(index)), query5.Types); return true; }
                if (queriers.TryQuery<T6>(context, out var query6)) { query = new Query<Any<T1, T2, T3, T4, T5, T6, T7>>(index => new Any<T1, T2, T3, T4, T5, T6, T7>(query6.Get(index)), query6.Types); return true; }
                if (queriers.TryQuery<T7>(context, out var query7)) { query = new Query<Any<T1, T2, T3, T4, T5, T6, T7>>(index => new Any<T1, T2, T3, T4, T5, T6, T7>(query7.Get(index)), query7.Types); return true; }
                query = default;
                return false;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();

        /// <summary>
        /// The value1.
        /// </summary>
        public readonly Maybe<T1> Value1;
        /// <summary>
        /// The value2.
        /// </summary>
        public readonly Maybe<T2> Value2;
        /// <summary>
        /// The value3.
        /// </summary>
        public readonly Maybe<T3> Value3;
        /// <summary>
        /// The value4.
        /// </summary>
        public readonly Maybe<T4> Value4;
        /// <summary>
        /// The value5.
        /// </summary>
        public readonly Maybe<T5> Value5;
        /// <summary>
        /// The value6.
        /// </summary>
        public readonly Maybe<T6> Value6;
        /// <summary>
        /// The value7.
        /// </summary>
        public readonly Maybe<T7> Value7;
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5, T6, T7}"/> struct.
        /// </summary>
        public Any(in T1 value) : this() { Value1 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5, T6, T7}"/> struct.
        /// </summary>
        public Any(in T2 value) : this() { Value2 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5, T6, T7}"/> struct.
        /// </summary>
        public Any(in T3 value) : this() { Value3 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5, T6, T7}"/> struct.
        /// </summary>
        public Any(in T4 value) : this() { Value4 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5, T6, T7}"/> struct.
        /// </summary>
        public Any(in T5 value) : this() { Value5 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5, T6, T7}"/> struct.
        /// </summary>
        public Any(in T6 value) : this() { Value6 = value; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Any{T1, T2, T3, T4, T5, T6, T7}"/> struct.
        /// </summary>
        public Any(in T7 value) : this() { Value7 = value; }
    }
}