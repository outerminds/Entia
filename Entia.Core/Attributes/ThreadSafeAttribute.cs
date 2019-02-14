using System;

namespace Entia.Core.Documentation
{
    /// <summary>
    /// Indicates that the target of the attribute is thread-safe.
    /// <para/>
    /// -> In the case of a type, it indicates that all its <c>public</c> members (static and instance) are thread-safe.
    /// <para/>
    /// -> In the case of a property or event, it indicates that all its <c>public</c> accessors are thread-safe.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Method |
        AttributeTargets.Property |
        AttributeTargets.Event |
        AttributeTargets.Class |
        AttributeTargets.Struct |
        AttributeTargets.Interface,
        AllowMultiple = false)]
    public sealed class ThreadSafeAttribute : Attribute { }
}