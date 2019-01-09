using Entia.Core;
using System;
using System.Reflection;

namespace Entia.Initializers
{
	public interface IInitializer
	{
		Result<Unit> Initialize(object instance, object[] instances);
	}

	public abstract class Initializer<T> : IInitializer
	{
		public abstract Result<Unit> Initialize(T instance, object[] instances);
		Result<Unit> IInitializer.Initialize(object instance, object[] instances) => Result.Cast<T>(instance).Bind(Initialize, instances).Box();
	}

	public sealed class Identity : IInitializer
	{
		public Result<Unit> Initialize(object instance, object[] instances) => Result.Success();
	}

	public sealed class Function<T> : Initializer<T>
	{
		readonly Func<T, object[], Result<Unit>> _initialize;
		public Function(Action<T> initialize) : this((instance, _) => { initialize(instance); return Result.Success(); }) { }
		public Function(Action<T, object[]> initialize) : this((instance, instances) => { initialize(instance, instances); return Result.Success(); }) { }
		public Function(Func<T, Result<Unit>> initialize) : this((instance, _) => initialize(instance)) { }
		public Function(Func<T, object[], Result<Unit>> initialize) { _initialize = initialize; }
		public override Result<Unit> Initialize(T instance, object[] instances) => _initialize(instance, instances);
	}

	public sealed class Array : Initializer<System.Array>
	{
		public readonly (int index, int reference)[] Items;

		public Array(params (int index, int reference)[] items) { Items = items; }

		public override Result<Unit> Initialize(System.Array instance, object[] instances)
		{
			try
			{
				foreach (var (index, reference) in Items) instance.SetValue(instances[reference], index);
				return Result.Success();
			}
			catch (Exception exception) { return Result.Exception(exception); }
		}
	}

	public sealed class Object : IInitializer
	{
		public readonly (FieldInfo field, int reference)[] Fields;
		public readonly (PropertyInfo property, int reference)[] Properties;

		public Object((FieldInfo field, int reference)[] fields = null, (PropertyInfo property, int reference)[] properties = null)
		{
			Fields = fields ?? new (FieldInfo, int)[0];
			Properties = properties ?? new (PropertyInfo, int)[0];
		}

		public Result<Unit> Initialize(object instance, object[] instances)
		{
			try
			{
				foreach (var (field, reference) in Fields) field.SetValue(instance, instances[reference]);
				foreach (var (property, reference) in Properties) property.SetValue(instance, instances[reference]);
				return Result.Success();
			}
			catch (Exception exception) { return Result.Exception(exception); }
		}
	}
}
