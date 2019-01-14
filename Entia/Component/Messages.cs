using System;
using Entia.Modules.Component;

namespace Entia.Messages
{
    public struct OnAdd : IMessage
    {
        public Entity Entity;
        public Metadata Component;
    }

    public struct OnRemove : IMessage
    {
        public Entity Entity;
        public Metadata Component;
    }

    public struct OnAdd<T> : IMessage where T : struct, IComponent { public Entity Entity; }
    public struct OnRemove<T> : IMessage where T : struct, IComponent { public Entity Entity; }
    public struct OnException : IMessage { public Exception Exception; }

    namespace Segment
    {
        public struct OnCreate : IMessage
        {
            public Modules.Component.Segment Segment;
        }

        public struct OnMove : IMessage
        {
            public Entity Entity;
            public (Modules.Component.Segment segment, int index) Source;
            public (Modules.Component.Segment segment, int index) Target;
        }
    }
}