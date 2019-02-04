using Entia.Builders;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Phases;
using System;
using System.Collections.Generic;

namespace Entia.Nodes
{
    public readonly struct Map : IWrapper
    {
        sealed class Builder : IBuilder
        {
            public Result<IRunner> Build(Node node, Node root, World world) => Result.Cast<Map>(node.Value)
                .Bind(data => world.Builders().Build(Node.Sequence(node.Name, node.Children), root)
                    .Map(child => data.Mapper(child)));
        }

        [Builder]
        static readonly Builder _builder = new Builder();

        public readonly Func<IRunner, IRunner> Mapper;
        public Map(Func<IRunner, IRunner> mapper) { Mapper = mapper; }
    }
}
