namespace Entia.Messages
{
    public struct OnAdopt : IMessage
    {
        public Entity Parent, Child;
        public int Index;
    }

    public struct OnReject : IMessage
    {
        public Entity Parent, Child;
        public int Index;
    }
}