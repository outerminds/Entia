using System;
using System.Reflection;
using Entia.Core;
using Entia.Injectables;
using Entia.Injectors;

namespace Entia.Injection
{
    public readonly struct Context
    {
        public readonly MemberInfo Member;
        public readonly World World;

        public Context(World world) : this(null, world) { }
        public Context(MemberInfo member, World world) { Member = member; World = world; }

        public Result<T> Inject<T>(MemberInfo member = null) where T : IInjectable =>
            Inject(typeof(T), member).Cast<T>();

        public Result<object> Inject(MemberInfo member)
        {
            var type = member as Type ??
                (member as FieldInfo)?.FieldType ??
                (member as PropertyInfo)?.PropertyType;
            if (type == null) return Result.Failure($"Expected member '{member}' to be a '{typeof(Type).Format()}', '{typeof(FieldInfo).Format()}' or '{typeof(PropertyInfo).Format()}'.");
            return Inject(type, member);
        }

        public Result<object> Inject(Type injectable, MemberInfo member = null) => World.Container.Get<IInjector>(injectable)
            .Select(With(member ?? injectable), (injector, state) => injector.Inject(state).As(injectable))
            .Choose()
            .FirstOrFailure();
        public Context With(MemberInfo member = null) => new Context(member ?? Member, World);
    }

    public static class Extensions
    {
        public static Result<T> Inject<T>(this World world, MemberInfo member) where T : IInjectable =>
            new Context(world).Inject<T>(member);
        public static Result<object> Inject(this World world, MemberInfo member) =>
            new Context(world).Inject(member);
        public static Result<object> Inject(this World world, Type injectable, MemberInfo member = null) =>
            new Context(world).Inject(injectable, member);
    }
}