using Entia.Core;
using Entia.Modules;
using System;
using System.Reflection;

namespace Entia.Injectors
{
    public interface IInjector
    {
        Result<object> Inject(MemberInfo member, World world);
    }

    public abstract class Injector<T> : IInjector
    {
        public abstract Result<T> Inject(MemberInfo member, World world);
        Result<object> IInjector.Inject(MemberInfo member, World world) => Inject(member, world).Box();
    }

    [AttributeUsage(ModuleUtility.AttributeUsage)]
    public sealed class InjectorAttribute : PreserveAttribute { }

    public sealed class Default : Injector<object>
    {
        public override Result<object> Inject(MemberInfo member, World world) =>
            Result.Failure($"No injector implementation was found for member '{member}'.");
    }

    public sealed class Provider<T> : Injector<T>
    {
        readonly Func<MemberInfo, World, Result<T>> _provider;
        public Provider(Func<MemberInfo, World, Result<T>> provider) { _provider = provider; }
        public override Result<T> Inject(MemberInfo member, World world) => _provider(member, world);
    }

    public static class Injector
    {
        public static Injector<T> From<T>(Func<MemberInfo, World, Result<T>> provider) => new Provider<T>(provider);
        public static Injector<T> From<T>(Func<World, Result<T>> provider) => new Provider<T>((_, world) => provider(world));
        public static Injector<T> From<T>(Func<World, T> provider) => new Provider<T>((_, world) => provider(world));
        public static Injector<T> From<T>(Func<T> provider) => new Provider<T>((_, __) => provider());
        public static Injector<T> From<T>(T instance) => new Provider<T>((_, __) => instance);
    }
}
