using Entia.Build;
using Entia.Builders;
using System;

namespace Entia.Messages
{
    public struct OnProfile : IMessage { public IRunner Runner; public Type Phase; public TimeSpan Elapsed; }
}
