using Entia.Core;
using Entia.Dependencies;
using Entia.Modules;
using System;
using System.Linq;
using System.Reflection;

namespace Entia.Dependers
{
	public interface IDepender
	{
		IDependency[] Depend(object target, MemberInfo member, World world);
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class DependerAttribute : PreserveAttribute { }

	public sealed class Default : IDepender
	{
		public IDependency[] Depend(object target, MemberInfo member, World world) =>
			member is Type type && TypeUtility.GetDefault(type) is IDependency dependency ?
			new IDependency[] { dependency } : new IDependency[0];
	}

	public sealed class Read<T> : IDepender
	{
		public IDependency[] Depend(object target, MemberInfo member, World world) => new IDependency[] { new Read(typeof(T)) };
	}

	public sealed class Write<T> : IDepender
	{
		public IDependency[] Depend(object target, MemberInfo member, World world) => new IDependency[] { new Write(typeof(T)) };
	}

	public sealed class Emit<T> : IDepender
	{
		public IDependency[] Depend(object target, MemberInfo member, World world) => new IDependency[] { new Emit(typeof(T)) };
	}

	public sealed class React<T> : IDepender
	{
		public IDependency[] Depend(object target, MemberInfo member, World world) => new IDependency[] { new React(typeof(T)) };
	}

	public sealed class Members : IDepender
	{
		public IDependency[] Depend(object target, MemberInfo member, World world) =>
			member is Type type ?
			type.GetMembers().SelectMany(world.Dependers().Dependencies).ToArray() :
			new IDependency[0];
	}

	public sealed class Generics : IDepender
	{
		public IDependency[] Depend(object target, MemberInfo member, World world) =>
			member is Type type ?
			type.Hierarchy()
				.Where(child => child.GetCustomAttributes(true).OfType<Dependables.GenericsAttribute>().Any())
				.SelectMany(child => child.GetGenericArguments())
				.SelectMany(world.Dependers().Dependencies)
				.ToArray() :
			new IDependency[0];
	}
}
