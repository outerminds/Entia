using Entia.Modules.Component;
using System;

namespace Entia.Messages
{
    /// <summary>
    /// Message emitted after a component has been added to an entity.
    /// </summary>
    /// <seealso cref="IMessage" />
    public struct OnAdd : IMessage
    {
        /// <summary>
        /// The entity that gained a component.
        /// </summary>
        public Entity Entity;
        /// <summary>
        /// The component type that was added.
        /// </summary>
        public Metadata Component;
    }

    /// <summary>
    /// Message emitted after a component of type <typeparamref name="T"/> has been added to an entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <seealso cref="IMessage" />
    public struct OnAdd<T> : IMessage where T : struct, IComponent
    {
        /// <summary>
        /// The entity that gained a component of type <typeparamref name="T"/>.
        /// </summary>
        public Entity Entity;
    }

    /// <summary>
    /// Message emitted after a component has been removed from an entity.
    /// </summary>
    /// <seealso cref="IMessage" />
    public struct OnRemove : IMessage
    {
        /// <summary>
        /// The entity that lost a component.
        /// </summary>
        public Entity Entity;
        /// <summary>
        /// The component type that was removed.
        /// </summary>
        public Metadata Component;
    }

    /// <summary>
    /// Message emitted after a component of type <typeparamref name="T"/> has been removed from an entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <seealso cref="IMessage" />
    public struct OnRemove<T> : IMessage where T : struct, IComponent
    {
        /// <summary>
        /// The entity that lost a component of type <typeparamref name="T"/>.
        /// </summary>
        public Entity Entity;
    }

    /// <summary>
    /// Message emitted after a component has been enabled.
    /// </summary>
    /// <seealso cref="IMessage" />
    public struct OnEnable : IMessage
    {
        /// <summary>
        /// The entity that had a component enabled.
        /// </summary>
        public Entity Entity;
        /// <summary>
        /// The component type that was enabled.
        /// </summary>
        public Metadata Component;
    }

    /// <summary>
    /// Message emitted after a component of type <typeparamref name="T"/> has been enabled.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <seealso cref="IMessage" />
    public struct OnEnable<T> : IMessage where T : struct, IComponent
    {
        /// <summary>
        /// The entity that had a component of type <typeparamref name="T"/> enabled.
        /// </summary>
        public Entity Entity;
    }

    /// <summary>
    /// Message emitted after a component has been disabled.
    /// </summary>
    /// <seealso cref="IMessage" />
    public struct OnDisable : IMessage
    {
        /// <summary>
        /// The entity that had a component disabled.
        /// </summary>
        public Entity Entity;
        /// <summary>
        /// The component type that was disabled.
        /// </summary>
        public Metadata Component;
    }

    /// <summary>
    /// Message emitted after a component of type <typeparamref name="T"/> has been disabled.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <seealso cref="IMessage" />
    public struct OnDisable<T> : IMessage where T : struct, IComponent
    {
        /// <summary>
        /// The entity that had a component of type <typeparamref name="T"/> disabled.
        /// </summary>
        public Entity Entity;
    }

    /// <summary>
    /// Message emitted after an exception is thrown.
    /// </summary>
    /// <seealso cref="IMessage" />
    public struct OnException : IMessage
    {
        /// <summary>
        /// The thrown exception.
        /// </summary>
        public Exception Exception;
    }

    namespace Segment
    {
        /// <summary>
        /// Message emitted after a <see cref="Modules.Component.Segment"/> is created.
        /// </summary>
        /// <seealso cref="IMessage" />
        public struct OnCreate : IMessage
        {
            /// <summary>
            /// The created segment.
            /// </summary>
            public Modules.Component.Segment Segment;
        }

        /// <summary>
        /// Message emitted after an entity has been moved.
        /// </summary>
        /// <seealso cref="IMessage" />
        public struct OnMove : IMessage
        {
            /// <summary>
            /// The moved entity.
            /// </summary>
            public Entity Entity;
            /// <summary>
            /// The source of the move.
            /// </summary>
            public (Modules.Component.Segment segment, int index) Source;
            /// <summary>
            /// The target of the move.
            /// </summary>
            public (Modules.Component.Segment segment, int index) Target;
        }
    }
}