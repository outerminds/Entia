using Entia.Builders;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Phases;
using System.Collections.Generic;

namespace Entia.Nodes
{
    public readonly struct Sequence : Node.IData
    {
        sealed class Builder : IBuilder
        {
            public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase
            {
                var children = new List<Runner<T>>(node.Children.Length);
                foreach (var child in node.Children)
                {
                    if (world.Builders().Build<T>(child, controller).TryValue(out var current))
                        children.Add(current);
                }

                var runners = children.ToArray();
                switch (runners.Length)
                {
                    case 0: return Option.None();
                    case 1: return runners[0];
                    default: return new Runner<T>(runners, (in T phase) => { foreach (var runner in runners) runner.Run(phase); });
                }
            }
        }

        [Builder]
        static readonly Builder _builder = new Builder();
    }
}
