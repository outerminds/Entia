﻿namespace Entia.Queryables
{
	public static class QueryableExtensions
	{
		public static bool TryGet<T>(this Maybe<T> item, out T value)
			where T : struct, IQueryable
		{
			value = item.Value;
			return item.Has;
		}

		public static void Deconstruct<T1, T2>(in this All<T1, T2> item, out T1 value1, out T2 value2)
			where T1 : struct, IQueryable where T2 : struct, IQueryable
		{
			value1 = item.Value1;
			value2 = item.Value2;
		}
		public static void Deconstruct<T1, T2>(in this Any<T1, T2> item, out Maybe<T1> value1, out Maybe<T2> value2)
			where T1 : struct, IQueryable where T2 : struct, IQueryable
		{
			value1 = item.Value1;
			value2 = item.Value2;
		}
		public static void Deconstruct<T1, T2, T3>(in this All<T1, T2, T3> item, out T1 value1, out T2 value2, out T3 value3)
			where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable
		{
			value1 = item.Value1;
			value2 = item.Value2;
			value3 = item.Value3;
		}
		public static void Deconstruct<T1, T2, T3>(in this Any<T1, T2, T3> item, out Maybe<T1> value1, out Maybe<T2> value2, out Maybe<T3> value3)
			where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable
		{
			value1 = item.Value1;
			value2 = item.Value2;
			value3 = item.Value3;
		}
		public static void Deconstruct<T1, T2, T3, T4>(in this All<T1, T2, T3, T4> item, out T1 value1, out T2 value2, out T3 value3, out T4 value4)
			where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable
		{
			value1 = item.Value1;
			value2 = item.Value2;
			value3 = item.Value3;
			value4 = item.Value4;
		}
		public static void Deconstruct<T1, T2, T3, T4>(in this Any<T1, T2, T3, T4> item, out Maybe<T1> value1, out Maybe<T2> value2, out Maybe<T3> value3, out Maybe<T4> value4)
			where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable
		{
			value1 = item.Value1;
			value2 = item.Value2;
			value3 = item.Value3;
			value4 = item.Value4;
		}
		public static void Deconstruct<T1, T2, T3, T4, T5>(in this All<T1, T2, T3, T4, T5> item, out T1 value1, out T2 value2, out T3 value3, out T4 value4, out T5 value5)
			where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable where T5 : struct, IQueryable
		{
			value1 = item.Value1;
			value2 = item.Value2;
			value3 = item.Value3;
			value4 = item.Value4;
			value5 = item.Value5;
		}
		public static void Deconstruct<T1, T2, T3, T4, T5>(in this Any<T1, T2, T3, T4, T5> item, out Maybe<T1> value1, out Maybe<T2> value2, out Maybe<T3> value3, out Maybe<T4> value4, out Maybe<T5> value5)
			where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable where T5 : struct, IQueryable
		{
			value1 = item.Value1;
			value2 = item.Value2;
			value3 = item.Value3;
			value4 = item.Value4;
			value5 = item.Value5;
		}
		public static void Deconstruct<T1, T2, T3, T4, T5, T6>(in this All<T1, T2, T3, T4, T5, T6> item, out T1 value1, out T2 value2, out T3 value3, out T4 value4, out T5 value5, out T6 value6)
			where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable where T5 : struct, IQueryable where T6 : struct, IQueryable
		{
			value1 = item.Value1;
			value2 = item.Value2;
			value3 = item.Value3;
			value4 = item.Value4;
			value5 = item.Value5;
			value6 = item.Value6;
		}
		public static void Deconstruct<T1, T2, T3, T4, T5, T6>(in this Any<T1, T2, T3, T4, T5, T6> item, out Maybe<T1> value1, out Maybe<T2> value2, out Maybe<T3> value3, out Maybe<T4> value4, out Maybe<T5> value5, out Maybe<T6> value6)
			where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable where T5 : struct, IQueryable where T6 : struct, IQueryable
		{
			value1 = item.Value1;
			value2 = item.Value2;
			value3 = item.Value3;
			value4 = item.Value4;
			value5 = item.Value5;
			value6 = item.Value6;
		}
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7>(in this All<T1, T2, T3, T4, T5, T6, T7> item, out T1 value1, out T2 value2, out T3 value3, out T4 value4, out T5 value5, out T6 value6, out T7 value7)
			where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable where T5 : struct, IQueryable where T6 : struct, IQueryable where T7 : struct, IQueryable
		{
			value1 = item.Value1;
			value2 = item.Value2;
			value3 = item.Value3;
			value4 = item.Value4;
			value5 = item.Value5;
			value6 = item.Value6;
			value7 = item.Value7;
		}
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7>(in this Any<T1, T2, T3, T4, T5, T6, T7> item, out Maybe<T1> value1, out Maybe<T2> value2, out Maybe<T3> value3, out Maybe<T4> value4, out Maybe<T5> value5, out Maybe<T6> value6, out Maybe<T7> value7)
			where T1 : struct, IQueryable where T2 : struct, IQueryable where T3 : struct, IQueryable where T4 : struct, IQueryable where T5 : struct, IQueryable where T6 : struct, IQueryable where T7 : struct, IQueryable
		{
			value1 = item.Value1;
			value2 = item.Value2;
			value3 = item.Value3;
			value4 = item.Value4;
			value5 = item.Value5;
			value6 = item.Value6;
			value7 = item.Value7;
		}
	}
}