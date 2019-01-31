using Entia.Core;
using Entia.Dependencies;
using Entia.Modules;
using Entia.Modules.Analysis;
using Entia.Nodes;
using System;
using System.Linq;

namespace Entia.Analyzers
{
    public interface IAnalyzer
    {
        Result<IDependency[]> Analyze(Node node, Node root, World world);
    }

    public abstract class Analyzer<T> : IAnalyzer where T : struct, IAnalyzable
    {
        public abstract Result<IDependency[]> Analyze(T data, Node node, Node root, World world);

        Result<IDependency[]> IAnalyzer.Analyze(Node node, Node root, World world) =>
            Result.Cast<T>(node.Value).Bind(data => Analyze(data, node, root, world));
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AnalyzerAttribute : PreserveAttribute { }

    public sealed class Default : IAnalyzer
    {
        public Result<IDependency[]> Analyze(Node node, Node root, World world) =>
            node.Children.Select(child => world.Analyzers().Analyze(child, root)).All().Map(
                children => children.SelectMany(_ => _).ToArray());
    }
}
