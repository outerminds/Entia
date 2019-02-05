using Entia.Core;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Nodes;
using Entia.Dependencies;
using Entia.Phases;
using System;
using System.Collections.Generic;
using Entia.Modules;

namespace Entia.Builders
{
    public interface IBuilder
    {
        Result<IRunner> Build(Node node, Node root, World world);
    }

    public abstract class Builder<T> : IBuilder where T : IRunner
    {
        public abstract Result<T> Build(Node node, Node root, World world);
        Result<IRunner> IBuilder.Build(Node node, Node root, World world) => Build(node, root, world).Cast<IRunner>();
    }

    [AttributeUsage(ModuleUtility.AttributeUsage)]
    public sealed class BuilderAttribute : PreserveAttribute { }

    public sealed class Default : IBuilder
    {
        public Result<IRunner> Build(Node node, Node root, World world) => Result.Exception(new NotImplementedException());
    }
}
