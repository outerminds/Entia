using Entia.Core;
using System;
using System.Reflection;

namespace Entia.Injectors
{
	public interface IInjector
	{
		Result<object> Inject(MemberInfo member, World world);
	}

	public abstract class Injector<T> : IInjector
	{
		public abstract Result<T> Inject(MemberInfo member, World world);
		Result<object> IInjector.Inject(MemberInfo member, World world) => Inject(member, world).Box();
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class InjectorAttribute : PreserveAttribute { }

	public sealed class Default : Injector<object>
	{
		public override Result<object> Inject(MemberInfo member, World world) =>
			Result.Failure($"No injector implementation was found for member '{member}'.");
	}
}
