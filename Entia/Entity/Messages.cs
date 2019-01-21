namespace Entia.Messages
{
    /// <summary>
    /// Message emitted every time an entity is created.
    /// </summary>
    /// <seealso cref="IMessage" />
    public struct OnCreate : IMessage
    {
        /// <summary>
        /// The created entity.
        /// </summary>
        public Entity Entity;
    }

    /// <summary>
    /// Message emitted just before an entity is destroyed.
    /// </summary>
    /// <seealso cref="IMessage" />
    public struct OnPreDestroy : IMessage
    {
        /// <summary>
        /// The entity about to be destroyed.
        /// </summary>
        public Entity Entity;
    }

    /// <summary>
    /// Message emitted just after an entity has been destroyed.
    /// </summary>
    /// <seealso cref="IMessage" />
    public struct OnPostDestroy : IMessage
    {
        /// <summary>
        /// The destroyed entity.
        /// </summary>
        public Entity Entity;
    }
}