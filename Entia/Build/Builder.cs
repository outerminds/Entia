using Entia.Build;
using Entia.Core;
using System;

namespace Entia.Builders
{
    public interface IBuilder : ITrait
    {
        Result<IRunner> Build(in Context context);
    }

    public abstract class Builder<T> : IBuilder where T : struct
    {
        public abstract Result<IRunner> Build(in T data, in Context context);
        Result<IRunner> IBuilder.Build(in Context context) => Result.Cast<T>(context.Node.Value)
            .Bind((@this: this, context), (data, state) => state.@this.Build(data, state.context));
    }
}
