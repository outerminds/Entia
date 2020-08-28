using Entia.Core;
using Entia.Injectables;
using Entia.Injection;
using System;
using System.Linq;
using System.Reflection;

namespace Entia.Injectors
{
    public interface IInjector : ITrait, IImplementation<IInjectable, Default>
    {
        Result<object> Inject(in Context context);
    }

    public abstract class Injector<T> : IInjector
    {
        public abstract Result<T> Inject(in Context context);
        Result<object> IInjector.Inject(in Context context) => Inject(context).Box();
    }

    public sealed class Default : IInjector
    {
        public Result<object> Inject(in Context context) => context.Member switch
        {
            Type type => Result.Cast<object>(DefaultUtility.Default(type))
                .Bind((type: type.GetData(), context), (instance, state) => Result.And(
                    state.type.InstanceFields
                        .Select(field => state.context.Inject(field).Do(value => field.Field.SetValue(instance, value)))
                        .All(),
                    state.type.InstanceProperties
                        .Where(property => property.Property.CanWrite)
                        .Select(property => state.context.Inject(property).Do(value => property.Property.SetValue(instance, value)))
                        .All())
                    .Return(instance)),
            FieldInfo field => context.Inject(field.FieldType),
            PropertyInfo property => context.Inject(property.PropertyType),
            _ => Result.Failure($"No injector implementation was found for member '{context.Member}'."),
        };
    }

    public sealed class Provider<T> : Injector<T>
    {
        readonly Func<Context, T> _provider;
        public Provider(Func<Context, T> provider) { _provider = provider; }
        public override Result<T> Inject(in Context context) => _provider(context);
    }

    public static class Injector
    {
        public static Injector<T> From<T>(Func<Context, T> provider) => new Provider<T>(context => provider(context));
        public static Injector<T> From<T>(Func<T> provider) => new Provider<T>(_ => provider());
        public static Injector<T> From<T>(T instance) => new Provider<T>(_ => instance);
    }
}
