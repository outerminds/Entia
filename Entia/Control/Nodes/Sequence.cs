using Entia.Builders;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Phases;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Nodes
{
    public readonly struct Sequence : INode
    {
        sealed class Runner : IRunner
        {
            public object Instance => Children;
            public readonly IRunner[] Children;
            public Runner(params IRunner[] children) { Children = children; }

            public IEnumerable<Type> Phases() => Children.SelectMany(child => child.Phases());
            public IEnumerable<Phase> Phases(Controller controller) => Children.SelectMany(child => child.Phases(controller));
            public Option<Runner<T>> Specialize<T>(Controller controller) where T : struct, IPhase
            {
                var children = Children
                    .TrySelect((IRunner child, out Runner<T> special) => child.Specialize<T>(controller).TryValue(out special))
                    .ToArray();
                switch (children.Length)
                {
                    case 0: return Option.None();
                    case 1: return children[0];
                    default: return new Runner<T>((in T phase) => { foreach (var child in children) child.Run(phase); });
                }
            }
        }

        sealed class Builder : IBuilder
        {
            public Result<IRunner> Build(Node node, Node root, World world) => node.Children.Length == 1 ?
                Result.Cast<Sequence>(node.Value).Bind(_ => world.Builders().Build(node.Children[0], root)) :
                Result.Cast<Sequence>(node.Value)
                    .Bind(_ => node.Children.Select(child => world.Builders().Build(child, root)).All())
                    .Map(children => new Runner(children))
                    .Cast<IRunner>();
        }

        [Builder]
        static readonly Builder _builder = new Builder();
    }
}
