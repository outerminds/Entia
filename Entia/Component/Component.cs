namespace Entia
{
    /// <summary>
    /// Tag interface that all components must implement.
    /// </summary>
    public interface IComponent { }

    namespace Components
    {
        public struct Debug : IComponent { public string Name; }
    }
}
