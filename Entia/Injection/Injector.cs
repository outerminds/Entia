using Entia.Core;
using Entia.Injectables;
using Entia.Injection;
using System;

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
        public Result<object> Inject(in Context context) =>
            Result.Failure($"No injector implementation was found for member '{context.Member}'.");
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
