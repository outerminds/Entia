using Entia.Builders;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Phases;
using System;

namespace Entia.Nodes
{
    public readonly struct Map : IWrapper
    {
        sealed class Builder : IBuilder
        {
            public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase
            {
                if (Option.Cast<Nodes.Map>(node.Value).TryValue(out var data) &&
                    world.Builders().Build<T>(Node.Sequence(node.Name, node.Children), controller).TryValue(out var runner))
                    return data.Mapper(runner).Cast<Runner<T>>();

                return Option.None();
            }
        }

        [Builder]
        static readonly Builder _builder = new Builder();

        public readonly Func<IRunner, Option<IRunner>> Mapper;

        public Map(Func<IRunner, Option<IRunner>> mapper) { Mapper = mapper; }
    }
}
