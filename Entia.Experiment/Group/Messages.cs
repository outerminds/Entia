namespace Entia.Messages.Segment
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