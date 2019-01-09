using Entia.Core;
using Entia.Modules;
using Entia.Systems;
using System;
using System.Linq;
using System.Reflection;

namespace Entia.Injectors
{
	public sealed class System : Injector<ISystem>
	{
		public override Result<ISystem> Inject(MemberInfo member, World world)
		{
			switch (member)
			{
				case Type type:
					return Result
						.Try(Activator.CreateInstance, type)
						.Cast<ISystem>()
						.Bind(instance => type.GetFields()
							.Select(field => world.Injectors().Inject(field).Do((object current) => field.SetValue(instance, current)))
							.All()
							.Return(instance));
				case FieldInfo field: return Inject(field.FieldType, world);
				default: return Failure.Empty;
			}
		}
	}
}
