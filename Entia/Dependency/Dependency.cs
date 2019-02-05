using Entia.Core;
using System;

namespace Entia.Dependencies
{
    public interface IDependency { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class IgnoreAttribute : Attribute { }

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
