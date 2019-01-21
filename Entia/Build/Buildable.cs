using Entia.Builders;

namespace Entia.Modules.Build
{
    /// <summary>
    /// Tag interface that all buildables must implement.
    /// </summary>
    public interface IBuildable { }
    /// <summary>
    /// Tag interface that links a buildable to its builder implementation.
    /// </summary>
    public interface IBuildable<T> : IBuildable where T : IBuilder, new() { }
}
