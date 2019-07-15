using Entia.Messages.Component;

namespace Entia
{
    /// <summary>
    /// Tag interface that all components must implement.
    /// </summary>
    public interface IComponent { }

    namespace Messages.Component
    {
        public interface IOnChange : IMessage { }
    }

    namespace Components
    {
        public interface ISilent : IComponent { }
        public interface ISilent<T> : IComponent where T : struct, IOnChange { }
        public interface IEnabled : IComponent { }

        public struct Debug : IEnabled, ISilent { public string Name; }
        readonly struct IsDisabled<T> : IEnabled, ISilent where T : struct, IComponent { }
    }
}
