using Entia.Analyzers;

namespace Entia.Modules.Analysis
{
    public interface IAnalyzable { }
    public interface IAnalyzable<T> : IAnalyzable where T : IAnalyzer, new() { }
}
