using Entia.Stores;
using System;

namespace Entia.Messages
{
    public struct OnAdd : IMessage
    {
        public Entity Entity;
        public Type Type;
        public (int global, int local) Index;
    }

    public struct OnRemove : IMessage
    {
        public Entity Entity;
        public Type Type;
        public (int global, int local) Index;
    }

    public struct OnAdd<T> : IMessage where T : struct { public Entity Entity; }
    public struct OnRemove<T> : IMessage where T : struct { public Entity Entity; }
    public struct OnResolve : IMessage { public IStore Store; }
    public struct OnException : IMessage { public Exception Exception; }
}