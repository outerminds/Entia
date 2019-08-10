using Entia.Build;
using Entia.Builders;
using Entia.Core;
using System;

namespace Entia.Nodes
{
    public readonly struct Map : IWrapper, IImplementation<Map.Builder>
    {
        sealed class Builder : Builder<Map>
        {
            public override Result<IRunner> Build(in Map data, in Context context) =>
                context.Build(Node.Sequence(context.Node.Name, context.Node.Children))
                    .Map(data.Mapper, (child, state) => state(child));
        }

        public readonly Func<IRunner, IRunner> Mapper;
        public Map(Func<IRunner, IRunner> mapper) { Mapper = mapper; }
    }
}
