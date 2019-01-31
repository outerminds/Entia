using Entia.Core;

namespace Entia.Instantiators
{
	public interface IInstantiator
	{
		Result<object> Instantiate(object[] instances);
	}

	public sealed class Constant : IInstantiator
	{
		public readonly object Value;
		public Constant(object value) { Value = value; }
		public Result<object> Instantiate(object[] instances) => Value;
	}

	public sealed class New<T> : IInstantiator where T : new()
	{
		public Result<object> Instantiate(object[] instances) => new T();
	}

	public sealed class Clone : IInstantiator
	{
		public readonly object Value;
		public Clone(object value) { Value = CloneUtility.Shallow(value); }
		public Result<object> Instantiate(object[] instances) => CloneUtility.Shallow(Value);
	}
}
