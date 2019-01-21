using Entia.Builders;
using Entia.Core;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Phases;
using System.Diagnostics;

namespace Entia.Nodes
{
    public readonly struct Profile : IWrapper
    {
        sealed class Builder : IBuilder
        {
            public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase
            {
                if (world.Builders().Build<T>(Node.Sequence(node.Name, node.Children), controller).TryValue(out var runner))
                {
                    var watch = new Stopwatch();
                    var messages = world.Messages();
                    return new Runner<T>(runner, (in T phase) =>
                    {
                        watch.Restart();
                        runner.Run(phase);
                        watch.Stop();
                        messages.Emit(new OnProfile { Node = node, Phase = typeof(T), Elapsed = watch.Elapsed });
                    });
                }

                return Option.None();
            }
        }

        [Builder]
        static readonly Builder _builder = new Builder();
    }
}
