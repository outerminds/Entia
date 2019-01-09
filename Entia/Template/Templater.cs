using Entia.Core;
using Entia.Initializers;
using Entia.Instantiators;
using Entia.Modules;
using Entia.Modules.Template;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Templaters
{
	public interface ITemplater
	{
		Result<IInitializer> Initializer(object value, Context context, World world);
		Result<IInstantiator> Instantiator(object value, Context context, World world);
	}

	public abstract class Templater<T> : ITemplater
	{
		public abstract Result<IInitializer> Initializer(T value, Context context, World world);
		public abstract Result<IInstantiator> Instantiator(T value, Context context, World world);
		Result<IInitializer> ITemplater.Initializer(object value, Context context, World world) =>
			Result.Cast<T>(value).Bind((casted, pair) => Initializer(casted, pair.context, pair.world), (context, world));
		Result<IInstantiator> ITemplater.Instantiator(object value, Context context, World world) =>
			Result.Cast<T>(value).Bind((casted, pair) => Instantiator(casted, pair.context, pair.world), (context, world));
	}

	[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
	public sealed class TemplaterAttribute : PreserveAttribute { }

	public sealed class Default : ITemplater
	{
		public Result<IInitializer> Initializer(object value, Context context, World world)
		{
			if (TypeUtility.IsPrimitive(value)) return new Identity();

			switch (value)
			{
				case System.Array array:
				{
					var items = new List<(int, int)>(array.Length);
					for (var i = 0; i < array.Length; i++)
					{
						var item = array.GetValue(i);
						if (TypeUtility.IsPrimitive(item)) continue;

						var result = world.Templaters().Template(item, context);
						if (result.TryFailure(out var failure)) return failure;
						if (result.TryValue(out var element)) items.Add((i, element.Reference));
					}
					return new Array(items.ToArray());
				}
				case object @object:
				{
					var fields = TypeUtility.GetFields(@object.GetType());
					var members = new List<(FieldInfo, int)>(fields.Length);
					for (var i = 0; i < fields.Length; i++)
					{
						var field = fields[i];
						var member = field.GetValue(@object);
						if (TypeUtility.IsPrimitive(member)) continue;

						var result = world.Templaters().Template(member, context);
						if (result.TryFailure(out var failure)) return failure;
						if (result.TryValue(out var element)) members.Add((field, element.Reference));
					}
					return new Object(members.ToArray());
				}
				default: return new Identity();
			}
		}

		public Result<IInstantiator> Instantiator(object value, Context context, World world)
		{
			if (TypeUtility.IsPrimitive(value)) return new Constant(value);
			return new Clone(value);
		}
	}
}
