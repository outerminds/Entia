using Entia.Core;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Entia.Dependency
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class IgnoreAttribute : Attribute { }

    public readonly struct Context
    {
        public readonly MemberInfo Member;
        public readonly HashSet<MemberInfo> Members;
        public readonly World World;

        public Context(HashSet<MemberInfo> members, World world) : this(null, members, world) { }
        public Context(MemberInfo member, HashSet<MemberInfo> members, World world)
        {
            Member = member;
            Members = members;
            World = world;
        }

        public IDependency[] Dependencies<T>() => Dependencies(typeof(T));
        public IDependency[] Dependencies(MemberInfo member)
        {
            var boxes = World.Boxes();
            if (boxes.TryGet<IDependency[]>(member, out var box)) return box.Value;

            var dependencies = Next(member).Distinct().ToArray();
            boxes.Set(member, dependencies);
            return dependencies;
        }

        public Context With(MemberInfo member = null) => new Context(member ?? Member, Members, World);

        IEnumerable<IDependency> Next(MemberInfo member)
        {
            if (member.IsDefined(typeof(IgnoreAttribute))) yield break;
            if (Members.Add(member))
            {
                switch (member)
                {
                    case Type type:
                        if (type.GetElementType() is Type element)
                        {
                            if (type.IsPointer) yield return new Write(element);
                            foreach (var dependency in Next(element)) yield return dependency;
                        }

                        foreach (var depender in World.Container.Get<IDepender>(type))
                            foreach (var dependency in depender.Depend(With(type))) yield return dependency;
                        break;
                    case FieldInfo field:
                        foreach (var dependency in Next(field.FieldType)) yield return dependency;
                        break;
                    case PropertyInfo property:
                        foreach (var dependency in Next(property.PropertyType)) yield return dependency;
                        break;
                    case EventInfo @event:
                        foreach (var dependency in Next(@event.EventHandlerType)) yield return dependency;
                        break;
                    case MethodInfo method:
                        foreach (var dependency in method.GetParameters()
                            .Select(parameter => parameter.ParameterType)
                            .Append(method.ReturnType)
                            .SelectMany(Next))
                            yield return dependency;
                        break;
                    case ConstructorInfo constructor:
                        foreach (var dependency in constructor.GetParameters()
                            .Select(parameter => parameter.ParameterType)
                            .SelectMany(Next))
                            yield return dependency;
                        break;
                }
            }
        }
    }

    public static class Extensions
    {
        public static IDependency[] Dependencies<T>(this World world) =>
            new Context(new HashSet<MemberInfo>(), world).Dependencies<T>();
        public static IDependency[] Dependencies(this World world, MemberInfo member) =>
            new Context(new HashSet<MemberInfo>(), world).Dependencies(member);
    }
}

namespace Entia.Dependencies
{
    public interface IDependency { }

    public readonly struct Unknown : IDependency { }
    public readonly struct Read : IDependency
    {
        public readonly Type Type;
        public Read(Type type) { Type = type; }
        public override string ToString() => $"{GetType().Format()}({Type.Format()})";
    }
    public readonly struct Write : IDependency
    {
        public readonly Type Type;
        public Write(Type type) { Type = type; }
        public override string ToString() => $"{GetType().Format()}({Type.Format()})";
    }
    public readonly struct Emit : IDependency
    {
        public readonly Type Type;
        public Emit(Type type) { Type = type; }
        public override string ToString() => $"{GetType().Format()}({Type.Format()})";
    }
    public readonly struct React : IDependency
    {
        public readonly Type Type;
        public React(Type type) { Type = type; }
        public override string ToString() => $"{GetType().Format()}({Type.Format()})";
    }
}
