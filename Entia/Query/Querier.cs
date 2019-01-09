using Entia.Core;
using Entia.Modules.Query;
using Entia.Queryables;
using System;

namespace Entia.Queriers
{
	public interface IQuerier
	{
		Type Type { get; }
		Query Query(World world);
	}

	public abstract class Querier<T> : IQuerier where T : struct, IQueryable
	{
		Type IQuerier.Type => typeof(T);

		public abstract Query<T> Query(World world);
		Query IQuerier.Query(World world) => Query(world);
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class QuerierAttribute : PreserveAttribute { }

	public sealed class Default<T> : Querier<T> where T : struct, IQueryable
	{
		public override Query<T> Query(World world) => new Query<T>(
			Filter.Empty,
			_ => false,
			(Entia.Entity _, out T value) => { value = default; return false; });
	}
}
