using Entia.Builders;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Phases;
using System;

namespace Entia.Nodes
{
    public readonly struct State : IWrapper
    {
        sealed class Builder : IBuilder
        {
            public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase
            {
                if (Option.Cast<Nodes.State>(node.Value).TryValue(out var data) &&
                    world.Builders().Build<T>(Node.Sequence(node.Name, node.Children), controller).TryValue(out var runner))
                    return new Runner<T>(runner, (in T phase) => { if (data.Get() == Controller.States.Enabled) runner.Run(phase); });

                return Option.None();
            }
        }

        [Builder]
        static readonly Builder _builder = new Builder();

        public readonly Func<Controller.States> Get;

        public State(Func<Controller.States> get) { Get = get; }
    }
}
