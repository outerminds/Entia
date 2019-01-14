using Entia.Core;

namespace Entia.Test
{
    public class ResolveWorld : Action<World, Model>
    {
        public override void Do(World value, Model model) => value.Resolve();
        public override string ToString() => GetType().Format();
    }
}
