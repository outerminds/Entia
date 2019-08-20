using Entia.Core;
using Entia.Dependencies;
using System.Linq;

namespace Entia.Analysis
{
    public interface IAnalyzer : ITrait, IImplementation<object, Default>
    {
        Result<IDependency[]> Analyze(in Context context);
    }

    public abstract class Analyzer<T> : IAnalyzer where T : struct
    {
        public abstract Result<IDependency[]> Analyze(in T data, in Context context);
        Result<IDependency[]> IAnalyzer.Analyze(in Context context) => Result.Cast<T>(context.Node.Value)
            .Bind((@this: this, context), (data, state) => state.@this.Analyze(data, state.context));
    }

    public sealed class Default : IAnalyzer
    {
        public Result<IDependency[]> Analyze(in Context context) =>
            context.Node.Children.Select(context, (child, state) => state.Analyze(child)).All().Map(
                children => children.SelectMany(_ => _).ToArray());
    }
}
