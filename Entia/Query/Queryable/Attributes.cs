using Entia.Dependables;
using Entia.Modules.Query;
using System;
using System.Linq;

namespace Entia.Queryables
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
	public abstract class QueryAttribute : Attribute, IDependable<Dependers.QueryAttribute>
	{
		public readonly Query Query;
		protected QueryAttribute(Query query) { Query = query; }
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
	public sealed class AllAttribute : QueryAttribute
	{
		public readonly Type[] Types;
		public AllAttribute(params Type[] types) : base(Query.All(types.Select(Query.From).ToArray())) { Types = types; }
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
	public sealed class AnyAttribute : QueryAttribute
	{
		public readonly Type[] Types;
		public AnyAttribute(params Type[] types) : base(Query.Any(types.Select(Query.From).ToArray())) { Types = types; }
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
	public sealed class NoneAttribute : QueryAttribute
	{
		public readonly Type[] Types;
		public NoneAttribute(params Type[] types) : base(Query.None(types.Select(Query.From).ToArray())) { Types = types; }
	}
}
