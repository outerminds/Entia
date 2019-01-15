using Entia.Core;
using Entia.Modules.Analysis;
using Entia.Modules.Build;
using Entia.Modules.Control;
using System;

namespace Entia.Nodes
{
    public interface IWrapper { }
    public interface IAtomic { }
    public struct Sequence : Node.IData, IBuildable<Builders.Sequence> { }
    public struct Parallel : Node.IData, IAtomic, IAnalyzable<Analyzers.Parallel>, IBuildable<Builders.Parallel> { }
    public struct Automatic : Node.IData, IAtomic, IBuildable<Builders.Automatic> { }
    public struct System : Node.IData, IAtomic, IAnalyzable<Analyzers.System>, IBuildable<Builders.System> { public Type Type; }
    public struct Profile : Node.IData, IBuildable<Builders.Profile>, IWrapper { }
    public struct Interval : Node.IData, IBuildable<Builders.Interval>, IWrapper { public TimeSpan Delay; public Func<TimeSpan> Time; }
    public struct State : Node.IData, IBuildable<Builders.State>, IWrapper { public Func<Controller.States> Get; }
    public struct Map : Node.IData, IBuildable<Builders.Map>, IWrapper { public Func<IRunner, Option<IRunner>> Mapper; }
    public struct Resolve : Node.IData, IAtomic, IBuildable<Builders.Resolve> { public Func<IRunner, Option<IRunner>> Mapper; }
}
