namespace Entia.Messages
{
    public struct OnAdopt : IMessage
    {
        public Entity Parent, Child;
    }

    public struct OnReject : IMessage
    {
        public Entity Parent, Child;
    }
}