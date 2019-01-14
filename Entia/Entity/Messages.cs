using System;

namespace Entia.Messages
{
    public struct OnCreate : IMessage { public Entity Entity; }
    public struct OnPreDestroy : IMessage { public Entity Entity; }
    public struct OnPostDestroy : IMessage { public Entity Entity; }
}