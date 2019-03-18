using Entia.Core;
using Entia.Initializers;
using Entia.Instantiators;

namespace Entia
{
    public sealed class Template<T> : ITemplate
    {
        readonly (IInstantiator instantiator, IInitializer initializer)[] _pairs;
        readonly object[] _instances;

        public Template(params (IInstantiator instantiator, IInitializer initializer)[] pairs)
        {
            _pairs = pairs;
            _instances = new object[pairs.Length];
        }

        public Result<T> Instantiate()
        {
            for (var i = 0; i < _pairs.Length; i++)
            {
                var (instantiator, _) = _pairs[i];
                var result = instantiator.Instantiate(_instances);
                if (result.TryValue(out var instance)) _instances[i] = instance;
                else if (result.TryFailure(out var failure)) return failure;
            }

            for (var i = _pairs.Length - 1; i >= 0; i--)
            {
                var (_, initializer) = _pairs[i];
                var result = initializer.Initialize(_instances[i], _instances);
                if (result.TryFailure(out var failure)) return failure;
            }

            var value = Result.Cast<T>(_instances[0]);
            _instances.Clear();
            return value;
        }

        Result<object> ITemplate.Instantiate() => Instantiate().Box();
    }

    public interface ITemplate
    {
        Result<object> Instantiate();
    }
}
