using Entia.Core;
using Entia.Dependencies;
using System.Linq;
using System.Reflection;

namespace Entia.Dependers
{
	public sealed class QueryAttribute : IDepender
	{
		public IDependency[] Depend(object target, MemberInfo member, World world) => target is Queryables.QueryAttribute attribute ?
			attribute.Query.Filter.Types
				.Select(type => new Read(type))
				.Prepend(new Read(typeof(Entity)))
				.Cast<IDependency>()
				.ToArray() :
			new IDependency[0];
	}
}
