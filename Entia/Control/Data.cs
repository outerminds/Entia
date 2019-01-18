using Entia.Core;
using Entia.Modules.Analysis;
using Entia.Modules.Build;
using Entia.Modules.Control;
using System;

namespace Entia.Nodes
{
    public interface IWrapper : Node.IData { }
    public interface IAtomic : Node.IData { }
    public struct Sequence : Node.IData, IBuildable<Builders.Sequence> { }
    public struct Parallel : IAtomic, IAnalyzable<Analyzers.Parallel>, IBuildable<Builders.Parallel> { }
    public struct Automatic : IAtomic, IBuildable<Builders.Automatic> { }
    public struct System : IAtomic, IAnalyzable<Analyzers.System>, IBuildable<Builders.System> { public Type Type; }
    public struct Profile : IWrapper, IBuildable<Builders.Profile> { }
    public struct State : IWrapper, IBuildable<Builders.State> { public Func<Controller.States> Get; }
    public struct Map : IWrapper, IBuildable<Builders.Map> { public Func<IRunner, Option<IRunner>> Mapper; }
    public struct Resolve : IAtomic, IBuildable<Builders.Resolve> { public Func<IRunner, Option<IRunner>> Mapper; }
}
