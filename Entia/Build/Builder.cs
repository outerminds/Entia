using Entia.Core;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Nodes;
using Entia.Phases;
using System;

namespace Entia.Builders
{
    public interface IBuilder
    {
        Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class BuilderAttribute : PreserveAttribute { }

    public sealed class Default : IBuilder
    {
        public Option<Runner<T>> Build<T>(Node node, Controller controller, World world) where T : struct, IPhase => Option.None();
    }
}
