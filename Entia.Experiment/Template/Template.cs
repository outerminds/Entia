using Entia.Core;
using Entia.Initializers;
using Entia.Instantiators;

namespace Entia
{
	public sealed class Template<T> : ITemplate
	{
		readonly (IInitializer initializer, IInstantiator instantiator)[] _pairs;

		public Template(params (IInitializer initializer, IInstantiator instantiator)[] pairs) { _pairs = pairs; }

		public Result<T> Instantiate()
		{
			var instances = new object[_pairs.Length];
			for (var i = 0; i < _pairs.Length; i++)
			{
				var (_, instantiator) = _pairs[i];
				var result = instantiator.Instantiate(instances);
				if (result.TryValue(out var instance)) instances[i] = instance;
				else if (result.TryFailure(out var failure)) return failure;
			}

			for (var i = _pairs.Length - 1; i >= 0; i--)
			{
				var (initializer, _) = _pairs[i];
				var result = initializer.Initialize(instances[i], instances);
				if (result.TryFailure(out var failure)) return failure;
			}

			return Result.Cast<T>(instances[0]);
		}

		public Result<T> Initialize(T value)
		{
			var instances = new object[_pairs.Length];
			instances[0] = value;

			for (var i = 1; i < _pairs.Length; i++)
			{
				var (_, instantiator) = _pairs[i];
				var result = instantiator.Instantiate(instances);
				if (result.TryValue(out var instance)) instances[i] = instance;
				else if (result.TryFailure(out var failure)) return failure;
			}

			for (var i = _pairs.Length - 1; i >= 0; i--)
			{
				var (initializer, _) = _pairs[i];
				var result = initializer.Initialize(instances[i], instances);
				if (result.TryFailure(out var failure)) return failure;
			}

			return Result.Cast<T>(instances[0]);
		}

		Result<object> ITemplate.Instantiate() => Instantiate().Box();
		Result<object> ITemplate.Initialize(object value) => Result.Cast<T>(value).Bind(Initialize).Box();
	}

	public interface ITemplate
	{
		Result<object> Instantiate();
		Result<object> Initialize(object value);
	}
}
