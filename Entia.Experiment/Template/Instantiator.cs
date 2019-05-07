using System;
using Entia.Core;

namespace Entia.Instantiators
{
    public interface IInstantiator
    {
        Result<object> Instantiate(object[] instances);
    }

    public abstract class Instantiator<T> : IInstantiator
    {
        public abstract Result<T> Instantiate(object[] instances);
        Result<object> IInstantiator.Instantiate(object[] instances) => Instantiate(instances).Box();
    }

    public sealed class Constant : IInstantiator
    {
        public readonly object Value;
        public Constant(object value) { Value = value; }
        public Result<object> Instantiate(object[] instances) => Value;
    }

    public sealed class Reference : IInstantiator
    {
        public readonly int Index;
        public Reference(int index) { Index = index; }
        public Result<object> Instantiate(object[] instances) => Result.Try(state => state.instances[state.index], (index: Index, instances));
    }

    public sealed class Clone : IInstantiator
    {
        public readonly object Value;
        public Clone(object value) { Value = CloneUtility.Shallow(value); }
        public Result<object> Instantiate(object[] instances) => CloneUtility.Shallow(Value);
    }
}
