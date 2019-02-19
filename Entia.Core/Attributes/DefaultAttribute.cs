using System;

namespace Entia.Core
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class DefaultAttribute : PreserveAttribute { }
}