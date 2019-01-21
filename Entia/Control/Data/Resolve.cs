using Entia.Builders;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Phases;
using System;

namespace Entia.Nodes
{
    public readonly struct Resolve : IWrapper
    {
        sealed class Builder : IBuilder
        {
            public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase
            {
                if (Option.Cast<Nodes.Resolve>(node.Value).TryValue(out var data) &&
                    world.Builders().Build<T>(Node.Sequence(node.Name, node.Children), controller).TryValue(out var runner))
                    return typeof(T).Is<IResolve>() ? new Runner<T>(data, (in T phase) => { runner.Run(phase); world.Resolve(); }) : runner;

                return Option.None();
            }
        }

        [Builder]
        static readonly Builder _builder = new Builder();
    }
}
