using System;
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

    public sealed class Factory<T> : IInstantiator where T : new()
    {
        public readonly Func<T> Create;
        public Factory(Func<T> create) { Create = create; }
        public Result<object> Instantiate(object[] instances) => Create();
    }

    public sealed class Reference : IInstantiator
    {
        public readonly int Index;
        public Reference(int index) { Index = index; }
        public Result<object> Instantiate(object[] instances) => Index < instances.Length ? instances[Index] : Result.Failure();
    }

    public sealed class Clone : IInstantiator
    {
        public readonly object Value;
        public Clone(object value) { Value = CloneUtility.Shallow(value); }
        public Result<object> Instantiate(object[] instances) => CloneUtility.Shallow(Value);
    }
}
