using Entia.Analyzers;

namespace Entia.Modules.Analysis
{
    /// <summary>
    /// Tag interface that all analyzables must implement.
    /// </summary>
    public interface IAnalyzable { }
    /// <summary>
    /// Tag interface that links an analyzable to its analyzer implementation.
    /// </summary>
    public interface IAnalyzable<T> : IAnalyzable where T : IAnalyzer, new() { }
}
