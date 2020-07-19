using System;
using Entia.Core;
using Entia.Initializers;
using Entia.Instantiators;

namespace Entia
{
    public sealed class Template<T> : ITemplate
    {
        public struct Disposable : IDisposable
        {
            public Result<T> Value => Result.Cast<T>(_instances[0]);

            object[] _instances;
            Template<T> _template;

            public Disposable(object[] instances, Template<T> template)
            {
                _instances = instances;
                _template = template;
            }

            public Result<Unit> Instantiate()
            {
                var pairs = _template._pairs;
                for (var i = 0; i < pairs.Length; i++)
                {
                    var (instantiator, _) = pairs[i];
                    var result = instantiator.Instantiate(_instances);
                    if (result.TryValue(out var instance)) _instances[i] = instance;
                    else return result.AsFailure();
                }

                return Result.Success();
            }

            public Result<Unit> Initialize()
            {
                var pairs = _template._pairs;
                for (var i = pairs.Length - 1; i >= 0; i--)
                {
                    var (_, initializer) = pairs[i];
                    var result = initializer.Initialize(_instances[i], _instances);
                    if (result.IsFailure()) return result;
                }

                return Result.Success();
            }

            public void Dispose()
            {
                _template._instances.Put(_instances);
                _instances = null;
                _template = null;
            }
        }

        readonly (IInstantiator instantiator, IInitializer initializer)[] _pairs;
        readonly Pool<object[]> _instances;

        public Template(params (IInstantiator instantiator, IInitializer initializer)[] pairs)
        {
            _pairs = pairs;
            _instances = new Pool<object[]>(() => new object[pairs.Length], dispose: instance => instance.Clear());
        }

        public Result<T> Instantiate()
        {
            using (var template = Use())
            {
                return Result.And(template.Instantiate(), template.Initialize(), template.Value)
                    .Map(values => values.Item3);
            }
        }

        public Disposable Use() => new Disposable(_instances.Take(), this);

        Result<object> ITemplate.Instantiate() => Instantiate().Box();
    }

    public interface ITemplate
    {
        Result<object> Instantiate();
    }
}
