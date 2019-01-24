using System;

namespace Entia.Core
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class ThreadSafeAttribute : Attribute { }
}